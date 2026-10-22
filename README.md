# University Course Management API — Задание 2

Вторая часть проекта University Course Management API. В рамках задания реализована система аутентификации и авторизации на базе JWT и ASP.NET Core Identity, ролевой доступ с проверкой владельца ресурсов, версионирование API (v1 / v2) и отдельное клиентское MVC-приложение на базе `IHttpClientFactory`.

## Стек технологий
- C# / .NET 8
- ASP.NET Core Web API & ASP.NET Core MVC
- Entity Framework Core 8 (SQLite)
- ASP.NET Core Identity
- JWT Bearer Authentication
- Asp.Versioning.Mvc 8.1
- AutoMapper 16.2
- Swashbuckle (Swagger/OpenAPI)

## Структура решения
- `UniversityApi` — основной REST API сервис (порт 5144).
- `UniversityClient` — MVC-клиент для взаимодействия с API через HTTP (порт 5200).
- `scripts/` — SQL-скрипты миграций и начальных данных (`init_database.sql`, `seed_data.sql`).

---

## Запуск проекта

### 1. Запуск Web API
```bash
dotnet run --project UniversityApi/UniversityApi.csproj --urls "http://localhost:5144"
```
База данных `university.db` создается автоматически через миграции при первом запуске вместе со всеми тестовыми пользователями и ролями.

- Swagger UI: http://localhost:5144/swagger  
  В выпадающем списке вверху справа можно переключаться между `v1` и `v2`. Для защищенных эндпоинтов используется кнопка `Authorize` (в формате `Bearer <токен>`).

### 2. Запуск MVC-клиента
В отдельном окне терминала:
```bash
dotnet run --project UniversityClient/UniversityClient.csproj --urls "http://localhost:5200"
```
- Веб-интерфейс: http://localhost:5200  
- Адрес API настраивается в `UniversityClient/appsettings.json` (по умолчанию `http://localhost:5144`). Если API недоступен, клиент выводит понятное сообщение об ошибке подключения.

---

## Тестовые пользователи

Все пользователи уже созданы в базе со следующими данными:

| Логин | Пароль | Роль | Описание |
|---|---|---|---|
| `admin` | `Admin123!` | Admin | Полный доступ, управление ролями |
| `teacher` | `Teacher123!` | Teacher | Преподаватель Евгений Герцен (курс 1) |
| `teacher2` | `Teacher123!` | Teacher | Преподаватель Фарида Абдолдина (курс 2) |
| `student` | `Student123!` | Student | Студент Аян Сериков (записан на курс 1) |
| `student2` | `Student123!` | Student | Студент Арман Данияров (записан на курс 2) |

Требования к паролю при регистрации новых пользователей: минимум 6 символов, наличие цифры и строчной буквы. При 5 неверных попытках входа учетная запись блокируется на 5 минут.

---

## Схема прохождения запроса к защищенному ресурсу

```
Клиент (HTTP Request)
  │
  ├─> ExceptionMiddleware (перехват ошибок, возврат ReturnResult 500)
  │
  ├─> CORS Policy ("AllowClientApps" разрешает вызовы с клиента :5200)
  │
  ├─> Authentication Middleware (JwtBearer)
  │     - Проверка подписи, Issuer, Audience, срока действия токена
  │     - Если токен отсутствует/невалиден/просрочен -> 401 Unauthorized
  │     - Если валиден -> формирование ClaimsPrincipal (Id, Name, Roles, TeacherId, StudentId)
  │
  ├─> Authorization Middleware
  │     - Проверка атрибутов [Authorize] и ролей
  │     - Если роли недостаточно -> 403 Forbidden
  │
  └─> Контроллер / Бизнес-логика (проверка владения ресурсом)
        - Преподаватель меняет оценку только на своем курсе (иначе -> 403 Forbidden)
        - Студент запрашивает только свои оценки (иначе -> 403 Forbidden)
        - Если все проверки пройдены -> 200 OK / 201 Created
```

---

## Реализованные сценарии безопасности

1. **Анонимный доступ к курсам:** `GET /api/v1/courses` возвращает `200 OK`.
2. **Анонимное создание:** `POST /api/v1/courses` возвращает `401 Unauthorized`.
3. **Неверный пароль:** `POST /api/auth/login` возвращает `401 Unauthorized`, токен не генерируется.
4. **Успешный вход:** `POST /api/auth/login` с верными данными возвращает JWT и данные пользователя.
5. **Просроченный токен:** отклоняется с `401 Unauthorized` благодаря настройке `ClockSkew = TimeSpan.Zero`.
6. **Попытка удаления курса студентом:** возвращает `403 Forbidden` (или `401` без токена), так как удаление доступно только Admin.
7. **Изменение оценки преподавателем на своем курсе:** `PUT /api/enrollments/1/grade` с токеном `teacher` возвращает `200 OK`.
8. **Изменение оценки преподавателем на чужом курсе:** `PUT /api/enrollments/1/grade` с токеном `teacher2` возвращает `403 Forbidden`.
9. **Запрос чужих оценок студентом:** `GET /api/enrollments/student/2` с токеном `student` (id=1) возвращает `403 Forbidden`. Свои оценки (`student/1`) возвращаются успешно.
10. **Назначение роли админом:** `POST /api/auth/assign-role` с токеном `admin` возвращает `200 OK`.
11. **Попытка студента назначить себе роль:** `POST /api/auth/assign-role` с токеном `student` возвращает `403 Forbidden`.
12. **Версионирование курсов:**
    - `GET /api/v1/courses` — базовый DTO (id, name, description, credits, teacherId, createdAt). Сохранена обратная совместимость с `/api/courses`.
    - `GET /api/v2/courses` — расширенный DTO (добавлен вложенный объект `teacher` и число записанных студентов `enrolledStudentsCount`).

---

## Основные эндпоинты

- **Auth:**
  - `POST /api/auth/login` — вход и получение JWT
  - `POST /api/auth/register` — регистрация нового студента
  - `GET /api/auth/me` — информация о текущем пользователе
  - `POST /api/auth/assign-role` — смена роли (только Admin)
- **Courses:**
  - `GET /api/v1/courses` и `GET /api/v1/courses/{id}` (доступны анонимно)
  - `GET /api/v2/courses` и `GET /api/v2/courses/{id}` (доступны анонимно, расширенный контракт)
  - `POST /api/v1/courses`, `DELETE /api/v1/courses/{id}` (Admin)
- **Enrollments:**
  - `GET /api/enrollments` — список записей (с фильтрацией по роли)
  - `GET /api/enrollments/student/{studentId}` — просмотр оценок (только свои для студента)
  - `POST /api/enrollments` — запись на курс
  - `PUT /api/enrollments/{id}/grade` — выставление оценки (преподаватель своего курса или Admin)
  - `DELETE /api/enrollments/{id}` — удаление записи (Admin)
- **Students & Teachers:**
  - `GET /api/students`, `GET /api/students/{id}`, `POST /api/students`, `PUT /api/students/{id}`, `DELETE /api/students/{id}`
  - `GET /api/teachers`, `GET /api/teachers/{id}`, `POST /api/teachers`, `DELETE /api/teachers/{id}`
