# 📚 PPK-Courses

**PPK-Courses** — веб-приложение на **ASP.NET Core с Razor Pages** для управления учебным процессом.  
Позволяет студентам просматривать расписание, записываться на курсы, оплачивать их, отслеживать оценки и прогресс.  
Преподаватели могут управлять курсами, выставлять оценки, анализировать посещаемость и экспортировать данные в Excel.  
Реализованы панели студентов и преподавателей с фильтрацией, статистикой, графиками и авторизацией через сессии.

## ✨ Возможности

- 🔐 **Аутентификация**: вход, выход, разграничение ролей (студент/преподаватель).
- 📅 **Расписание**: просмотр, фильтрация по группам, датам, курсам, запись на занятия.
- 📊 **Оценки**: выставление, просмотр, экспорт в Excel, аналитика (средний балл, прогресс).
- 📈 **Статистика**: графики успеваемости и посещаемости, топ-5 студентов.
- 💳 **Оплата курсов**: валидация платежей, запись на платные курсы.
- 🛠️ **Панель преподавателя**: управление курсами, оценками, посещаемостью, экспорт.
- 🎨 **Современный интерфейс**: адаптивный дизайн, таблицы, модалки, графики на Chart.js.

## 📋 Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- SQL Server или SQL Server Express
- Любой современный браузер (Chrome, Firefox, Edge)
- [EPPlus](https://github.com/EPPlusSoftware/EPPlus) для экспорта в Excel (нужна настройка лицензии)

## 🧩 Зависимости

| Технология / Библиотека         | Назначение                              |
|----------------------------------|------------------------------------------|
| `ASP.NET Core Razor Pages`       | Основной веб-фреймворк                   |
| `Entity Framework Core`          | ORM для работы с БД                      |
| `EPPlus`                         | Экспорт данных в Excel                   |
| `Chart.js`                       | Построение графиков и статистики         |
| `Microsoft.Data.SqlClient`       | SQL Server-драйвер                       |
| `Dapper`                         | Быстрые SQL-запросы                      |

Полный список зависимостей — в файле `web.csproj`.

## 🚀 Установка и запуск

1. **Клонируйте репозиторий**
   ```bash
   git clone https://github.com/TemhaN/PPK-Courses.git
   cd PPK-Courses

2. **Настройте строку подключения**

   В `appsettings.json` укажите свою строку подключения:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your_server;Database=sabzor04_db;Integrated Security=True;TrustServerCertificate=True;"
     }
   }
   ```

3. **Установите зависимости**

   ```bash
   dotnet restore
   ```

4. **Примените миграции**

   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

   Если базы ещё нет — она создастся автоматически при запуске.

5. **Настройте EPPlus**
   В `Program.cs` добавьте:

   ```csharp
   ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
   ```

   Или установите переменную окружения:

   ```
   EPPLUS_LICENSE=NonCommercial
   ```

6. **Запустите приложение**

   ```bash
   dotnet run
   ```

   Открой в браузере: [http://localhost:5000](http://localhost:5000)

## 🖱️ Использование

### 🔑 Аутентификация

* Вход через `/Login`.
* Роли (студент / преподаватель) сохраняются в сессии (`UserRole`).

### 🎓 Панель студента (`/StudentDashboard`)

* Просмотр профиля, оценок, прогресса, ближайших занятий.
* Запись на курсы на странице `/Schedule` (с фильтрацией и оплатой).

### 👨‍🏫 Панель преподавателя (`/TeacherDashboard`)

* Просмотр статистики: средние оценки, посещаемость, топ студентов.
* Фильтрация по курсам и группам.
* Экспорт расписания и оценок в Excel.

### 📤 Экспорт в Excel

* Кнопка **"Экспорт в Excel"** в панели преподавателя.
* Доступен также экспорт расписания на странице `/Schedule`.

## 📦 Сборка и деплой

### 🔧 Сборка релизной версии

```bash
dotnet publish -c Release -o ./publish
```

### 🚀 Развёртывание

* Скопируй содержимое папки `publish` на сервер.
* Настрой веб-сервер (IIS или Kestrel).
* Убедись, что SQL Server доступен, а строка подключения актуальна.

## 📸 Скриншоты

<div style="display: flex; flex-wrap: wrap; gap: 10px; justify-content: center;">
  <img src="https://github.com/TemhaN/ppk-courses/blob/master/web/Screenshots/1.png?raw=true" alt="ppk-courses" width="30%">
  <img src="https://github.com/TemhaN/ppk-courses/blob/master/web/Screenshots/2.png?raw=true" alt="ppk-courses" width="30%">
  <img src="https://github.com/TemhaN/ppk-courses/blob/master/web/Screenshots/3.png?raw=true" alt="ppk-courses" width="30%">
  <img src="https://github.com/TemhaN/ppk-courses/blob/master/web/Screenshots/4.png?raw=true" alt="ppk-courses" width="30%">
  <img src="https://github.com/TemhaN/ppk-courses/blob/master/web/Screenshots/5.png?raw=true" alt="ppk-courses" width="30%">
  <img src="https://github.com/TemhaN/ppk-courses/blob/master/web/Screenshots/6.png?raw=true" alt="ppk-courses" width="30%">
  <img src="https://github.com/TemhaN/ppk-courses/blob/master/web/Screenshots/7.png?raw=true" alt="ppk-courses" width="30%">
  <img src="https://github.com/TemhaN/ppk-courses/blob/master/web/Screenshots/8.png?raw=true" alt="ppk-courses" width="30%">
  <img src="https://github.com/TemhaN/ppk-courses/blob/master/web/Screenshots/9.png?raw=true" alt="ppk-courses" width="30%">
</div>    

## 🧠 Автор

**TemhaN**  
[GitHub профиль](https://github.com/TemhaN)

## 🧾 Лицензия

Проект распространяется под лицензией [MIT License].

## 📬 Обратная связь

Нашли баг или хотите предложить улучшение?
Создайте **issue** или присылайте **pull request** в репозиторий!

## ⚙️ Технологии

* **ASP.NET Core Razor Pages** — веб-фреймворк
* **Entity Framework Core** — ORM
* **SQL Server** — база данных
* **EPPlus** — экспорт Excel
* **Chart.js** — графики и статистика
* **Dapper** — быстрые SQL-запросы
* **Bootstrap / Tailwind** — стилизация интерфейса
* **Session-based Authentication** — сессионная авторизация

