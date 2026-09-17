# System rezerwacji sal na uczelni

ASP.NET Core 8 MVC + Entity Framework Core + SQL Server LocalDB + ASP.NET Core Identity.

## Как запустить (Visual Studio)

1. В **Visual Studio Installer** должен стоять workload **ASP.NET and web development**.
2. Открой `SalaRezerwacja.sln`.
3. `Build → Restore NuGet Packages` (или просто собери решение — пакеты подтянутся сами).
4. Строка подключения уже настроена на LocalDB в `appsettings.json`. Если у тебя SQL Server Express, поменяй `Server=(localdb)\mssqllocaldb` на `Server=.\SQLEXPRESS`.
5. Открой `Tools → NuGet Package Manager → Package Manager Console` и выполни:
   ```
   Add-Migration InitialCreate
   ```
   Команду `Update-Database` запускать не нужно — миграции применяются автоматически при старте (`DbSeeder.SeedAsync`).
6. Нажми **F5**.

## Тестовые аккаунты

Создаются автоматически при первом запуске, пароль у всех `Haslo123!`:

| Логин | Роль | Что может |
|---|---|---|
| `admin@uczelnia.edu.pl` | Administracja | всё, включая отчёты и импорт расписания |
| `opiekun@uczelnia.edu.pl` | Opiekun | добавление и редактирование аудиторий, отчёты |
| `student@uczelnia.edu.pl` | Student | бронирование и отмена своих броней |

Вход по «вузовскому аккаунту»: `/UniversityAuth/Login`, любой адрес в домене `uczelnia.edu.pl` и пароль от 6 символов (это симуляция, см. ниже).

## Где какое требование закрыто

| Требование | Файлы |
|---|---|
| 1. Логин через вузовский аккаунт | `Services/UniversityAuthProvider.cs`, `Controllers/UniversityAuthController.cs` |
| 1. Обычная регистрация и логин | ASP.NET Core Identity UI (`/Identity/Account/Login`), настройка в `Program.cs` |
| 2. База аудиторий | `Models/Room.cs`, `Models/Building.cs`, `Models/Equipment.cs`, `Data/ApplicationDbContext.cs` |
| 2. Добавление/редактирование опекуном | `Controllers/RoomsController.cs` (атрибут `[Authorize(Roles = ...)]`, метод `CanEdit`) |
| 3. Бронирование на дату и время | `Services/ReservationService.CreateAsync` |
| 3. Лимит времени по аудитории | `Room.MaxReservationMinutes` + проверка в `CreateAsync` |
| 3. Уведомления e-mail/SMS | `Services/NotificationService.cs`, `Services/ReminderBackgroundService.cs` |
| 3. Отмена до начала | `Reservation.CanBeCancelled`, `ReservationService.CancelAsync` |
| 4. Календарь занятости | `Views/Calendar/Index.cshtml` (FullCalendar) + `CalendarController.Events` |
| 4. Фильтры | `Models/ViewModels/RoomSearchViewModel.cs`, `ReservationService.SearchAsync` / `GetCalendarAsync` |
| 4. Синхронизация с расписанием | `Services/ScheduleImportService.cs`, `Views/Calendar/Import.cshtml` |
| 5. Поиск свободных аудиторий | `ReservationService.SearchAsync`, `Views/Rooms/Index.cshtml` |
| 6. Отчёты и статистика | `Services/ReportService.BuildAsync`, `Views/Reports/Index.cshtml` |
| 6. Экспорт CSV / PDF | `ReportService.ToCsv` (CsvHelper), `ReportService.ToPdf` (QuestPDF) |

## Ключевая логика — проверка пересечения броней

`Services/ReservationService.IsRoomFreeAsync`:

```csharp
start < r.EndTime && end > r.StartTime
```

Два интервала пересекаются тогда и только тогда, когда новый начинается раньше, чем заканчивается существующий, и заканчивается позже, чем существующий начинается. Проверка идёт на сервере перед сохранением, а не только в интерфейсе.

## Что честно сказать на защите

**Вход по вузовскому аккаунту реализован как интерфейс + заглушка.** Реальное подключение к Microsoft Entra ID (Azure AD), CAS, LDAP или USOS API требует client ID и secret, которые выдаёт IT-отдел вуза. В коде есть `IUniversityAuthProvider` — чтобы подключить настоящего провайдера, достаточно написать вторую реализацию и заменить одну строку в `Program.cs`. Всё остальное приложение менять не придётся. Подробный комментарий с вариантами протоколов — в `Services/UniversityAuthProvider.cs`.

**Синхронизация с расписанием** сделана импортом файлов iCalendar (.ics), потому что публичного API расписания у вузов обычно нет. Импортированные занятия отображаются в календаре и блокируют бронирование аудитории.

**SMS** отключены в `appsettings.json` (`Sms:Enabled = false`) — в `NotificationService` есть точка интеграции с Twilio с комментарием. E-mail работает, если указать SMTP-сервер и выставить `Smtp:Enabled = true`; иначе текст письма пишется в лог, это видно в окне Output в Visual Studio.

## Что можно доделать, если попросят

- Добавить поля «Имя» и «Фамилия» в форму регистрации: `Add → New Scaffolded Item → Identity`, выбрать страницу `Account\Register`, затем дописать поля в сгенерированный `Register.cshtml.cs`.
- Повторяющиеся брони (каждую неделю на семестр).
- Экспорт брони в .ics, чтобы пользователь добавил её в свой календарь.
- Юнит-тесты на `ReservationService` — там чистая логика без UI, тестируется легко.
