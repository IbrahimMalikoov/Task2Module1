# Satbayev University Course Management API (Задание 2, Модули 5-8)

CSE5032 Разработка веб-сервисов  
Зав. кафедры ПИ: Абдолдина Ф. Н.  
Экзаменатор: Герцен Е. А.

---

## 1. Структура решения

Решение состоит из двух независимых проектов на платформе **.NET 8**:
* **`UniversityApi`** — Безопасный версионируемый RESTful Web API с аутентификацией JWT, ASP.NET Core Identity, ролевой авторизацией, политикой принадлежности данных и OpenAPI/Swagger (v1 и v2).
* **`UniversityClient`** — Клиентское ASP.NET Core MVC приложение, использующее `IHttpClientFactory` для обращения к API, с конфигурацией адреса через `appsettings.json` и обработкой сетевых ошибок / кодов ответов.

---

## 2. Схема прохождения HTTP-запроса к защищенному ресурсу

```
[Клиент: Браузер / Swagger / Postman / UniversityClient]
                       │
                       │ 1. HTTP-запрос с заголовком:
                       │    Authorization: Bearer <JWT-Token>
                       ▼
       ┌───────────────────────────────┐
       │     ExceptionMiddleware       │  (Перехват необработанных исключений -> ReturnResult 500)
       └───────────────┬───────────────┘
                       │
                       ▼
       ┌───────────────────────────────┐
       │        CorsMiddleware         │  (Проверка Origin: localhost:5200, localhost:7200)
       └───────────────┬───────────────┘
                       │
                       ▼
       ┌───────────────────────────────┐
       │   Authentication Middleware   │
       │    (JwtBearerHandler)         │
       └───────────────┬───────────────┘
                       │
        [Токен валиден? Подпись/Issuer/Срок]
              │                  │
             Нет                 Да
              │                  │
              ▼                  ▼
       [401 Unauthorized]  [Формирование ClaimsPrincipal:
       (ReturnResult)       NameIdentifier, Name, Role,
                            TeacherId, StudentId]
                                 │
                                 ▼
                   ┌───────────────────────────┐
                   │ Authorization Middleware  │
                   │   ([Authorize], Roles)    │
                   └─────────────┬─────────────┘
                                 │
                   [Роль и права соответствуют?]
                         │               │
                        Нет              Да
                         │               │
                         ▼               ▼
                  [403 Forbidden] [Контроллер / Бизнес-логика]
                  (ReturnResult)         │
                                         ▼
                             [Проверка владельца ресурса:]
                             - TeacherId совпадает с курсом?
                             - StudentId совпадает со студентом?
                                   │              │
                                  Нет             Да
                                   │              │
                                   ▼              ▼
                            [403 Forbidden] [200 OK / 201 Created]
                            (ReturnResult)   (Данные без секретов)
```

---

## 3. Инструкция по сборке и запуску

### Требования
* .NET SDK 8.0 (версия 8.0.x)

### Сборка решения
```bash
dotnet build UniversityApi.sln
```

### Запуск Web API
```bash
dotnet run --project UniversityApi/UniversityApi.csproj --urls "http://localhost:5144"
```
* **Swagger UI:** `http://localhost:5144/swagger` (доступны спецификации `v1` и `v2`).
* База данных SQLite (`university.db`) и тестовые учетные записи инициализируются автоматически при первом старте через миграции EF Core.

### Запуск MVC-клиента (в отдельном терминале)
```bash
dotnet run --project UniversityClient/UniversityClient.csproj --urls "http://localhost:5200"
```
* **Веб-интерфейс клиента:** `http://localhost:5200`
* Настройки подключения к API хранятся в `UniversityClient/appsettings.json`:
  ```json
  "ApiSettings": {
    "BaseUrl": "http://localhost:5144"
  }
  ```

---

## 4. Тестовые пользователи и учетные данные

При старте приложения автоматически создаются следующие пользователи:

| Логин | Пароль | Роль | Связанная сущность | Назначение |
|---|---|---|---|---|
| `admin` | `Admin123!` | `Admin` | — | Управление пользователями и ролями, полный доступ |
| `teacher` | `Teacher123!` | `Teacher` | Преподаватель Евгений Герцен (ID: 1) | Ведет курс 1 (CSE5032) |
| `teacher2` | `Teacher123!` | `Teacher` | Преподаватель Фарида Абдолдина (ID: 2) | Ведет курс 2 (CSE5021) |
| `student` | `Student123!` | `Student` | Студент Аян Сериков (ID: 1) | Записан на курс 1 |
| `student2` | `Student123!` | `Student` | Студент Арман Данияров (ID: 2) | Записан на курс 2 |

---

## 5. Проверка комплексных сценариев безопасности (Раздел 7)

| № | Сценарий | HTTP-метод и Endpoint | Токен / Пользователь | Ожидаемый результат | Фактический результат |
|---|---|---|---|---|---|
| 1 | Анонимный пользователь запрашивает список курсов | `GET /api/v1/courses` | Без токена | `200 OK` | `200 OK` |
| 2 | Анонимный пользователь пытается создать курс | `POST /api/v1/courses` | Без токена | `401 Unauthorized` | `401 Unauthorized` |
| 3 | Пользователь вводит неправильный пароль | `POST /api/auth/login` | Неверный пароль | Ошибка аутентификации без JWT | `401 Unauthorized`, JWT не выдается |
| 4 | Пользователь входит с правильными данными | `POST /api/auth/login` | `admin` / `Admin123!` | Получает JWT | `200 OK`, выдан валидный JWT токен |
| 5 | Пользователь обращается с просроченным JWT | `GET /api/auth/me` | Expired Bearer Token | `401 Unauthorized` | `401 Unauthorized` (ClockSkew = 0) |
| 6 | Студент пытается удалить курс | `DELETE /api/v1/courses/1` | Токен `student` / без токена | `401 / 403` | `403 Forbidden` (с токеном) / `401 Unauthorized` (без) |
| 7 | Преподаватель изменяет оценку на собственном курсе | `PUT /api/enrollments/1/grade` | Токен `teacher` (курс 1) | `200 OK` | `200 OK`, оценка обновлена |
| 8 | Преподаватель изменяет оценку на чужом курсе | `PUT /api/enrollments/1/grade` | Токен `teacher2` (курс 2) | `403 Forbidden` | `403 Forbidden`, отказ в доступе |
| 9 | Студент запрашивает чужие оценки | `GET /api/enrollments/student/2` | Токен `student` (ID: 1) | `403 Forbidden` | `403 Forbidden`, отказ в доступе |
| 10 | Admin назначает пользователю роль | `POST /api/auth/assign-role` | Токен `admin` | Успешное изменение роли | `200 OK`, роль обновлена |
| 11 | Студент пытается назначить себе роль Admin | `POST /api/auth/assign-role` | Токен `student` | `403 Forbidden` | `403 Forbidden`, запрет повышения прав |
| 12 | Клиент обращается к API v1 и v2 | `GET /api/v1/courses`, `GET /api/v2/courses` | Любой пользователь | Обе версии возвращают данные | `200 OK`: v1 — базовый контракт, v2 — контракт с Teacher и количеством студентов |

---

## 6. Теоретические вопросы к защите

### Взаимосвязь компонентов безопасности в ASP.NET Core:
1. **ASP.NET Core Identity**:
   Система управления пользователями, паролями (хеширование PBKDF2), блокировками (Lockout) и ролями. Интегрирована с Entity Framework Core через `ApplicationDbContext : IdentityDbContext<ApplicationUser>`.
2. **JWT (JSON Web Token)**:
   Компактный самодостаточный формат передачи утверждений (Claims). При логине сервер подписывает токен симметричным ключом (HMAC-SHA256). Токен содержит `sub` (UserId), `name`, `role`, а также идентификаторы привязанных профилей (`TeacherId`, `StudentId`).
3. **Authentication Middleware**:
   Выполняется на каждом запросе. Извлекает токен из заголовка `Authorization: Bearer <token>`, проверяет цифровую подпись, валидирует `Issuer`, `Audience` и срок действия (`exp`). В случае успеха создает объект `ClaimsPrincipal` и помещает его в `HttpContext.User`. Если токен недействителен или отсутствует — инициирует Challenge (`401 Unauthorized`).
4. **Authorization Middleware**:
   Выполняется после Authentication Middleware. Проверяет декларативные правила доступа (`[Authorize]`, `Roles = "Admin"`, политики). Если `HttpContext.User` не аутентифицирован — отправляет Challenge (`401 Unauthorized`). Если пользователь аутентифицирован, но не входит в требуемую роль — отправляет Forbid (`403 Forbidden`).
5. **Claims и Roles**:
   Role является частным случаем Claim (`ClaimTypes.Role`). Клеймы несут дополнительную информацию о контексте пользователя (`TeacherId`, `StudentId`).

### Почему одной проверки роли недостаточно для защиты данных?
Проверка роли (например, `[Authorize(Roles = "Teacher")]`) гарантирует лишь то, что пользователь является преподавателем в системе, но **не гарантирует его право на работу с конкретным курсом или студентом**.
Без проверки принадлежности данных (Resource-based Authorization):
* Преподаватель мог бы изменять оценки на курсах других преподавателей (уязвимость IDOR / Broken Object Level Authorization).
* Студент мог бы просматривать оценки и персональные данные других студентов.
В данном проекте реализована проверка принадлежности на уровне контроллеров:
* `EnrollmentsController.UpdateGrade`: проверяет, что `enrollment.Course.TeacherId == currentTeacherId`.
* `EnrollmentsController.GetByStudentId`: проверяет, что запрашиваемый `studentId == currentStudentId`.
