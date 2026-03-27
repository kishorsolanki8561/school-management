# API Reference

Base URL: `https://{host}/api/v1`

All protected endpoints require: `Authorization: Bearer {accessToken}`

### Audit Tracking Header

To record which screen triggered a change, include this header on any write request (POST / PUT / DELETE):

```
X-Screen-Name: Country Management
```

The value is stored in `AuditLogs.ScreenName`. If omitted the field is stored as `null`.

---

## Response Format

### Success
```json
{
  "success": true,
  "message": "string",
  "data": { ... },
  "errors": null
}
```

### Error
```json
{
  "success": false,
  "message": "string",
  "data": null,
  "errors": [
    { "field": "Name", "message": "Name is required" }
  ]
}
```

### Paginated Response (`data` field)
```json
{
  "items": [ ... ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

---

## Auth Module

### POST `/auth/login`
Authenticate and receive tokens.

**Request**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response `data`**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123...",
  "accessTokenExpiry": "2026-03-23T10:00:00Z",
  "username": "johndoe",
  "role": "Super Admin"
}
```

---

### POST `/auth/register`
Register a new user account.

**Request**
```json
{
  "username": "string",
  "email": "string",
  "password": "string",
  "roleIds": [1, 2],
  "orgIds": [5, 6]
}
```

> `roleIds` is optional. Each ID must match an existing `Roles.Id` (1=Owner Admin, 2=Super Admin, 3=Admin). One `UserRoleMapping` row is created per entry.
> `orgIds` is optional. Each ID must match an existing `Organizations.Id`. One `UserOrganizationMapping` row is created per entry — a user can belong to multiple organisations simultaneously.

---

### POST `/auth/refresh`
Exchange a valid refresh token for new tokens.

**Request**
```json
{
  "refreshToken": "string"
}
```

**Response `data`** — same shape as login response.

---

### POST `/auth/logout`
Revoke the current refresh token. **Requires auth.**

**Request**
```json
{
  "refreshToken": "string"
}
```

---

### POST `/auth/forgot-password`
Request a password reset email.

**Request**
```json
{
  "email": "string"
}
```

---

### POST `/auth/reset-password`
Reset password using the emailed token.

**Request**
```json
{
  "token": "string",
  "newPassword": "string"
}
```

---

## Country Module

All endpoints require auth (`Bearer`).

### POST `/country`
Create a new country.

**Request**
```json
{
  "name": "India",
  "code": "IND"
}
```

**Response `data`** — `CountryResponse` (see [MASTERS.md](MASTERS.md))

---

### PUT `/country/{id}`
Update an existing country.

**Request**
```json
{
  "name": "India",
  "code": "IND",
  "isActive": true
}
```

---

### DELETE `/country/{id}`
Soft-delete a country (`IsDeleted = true`).

---

### GET `/country/{id}`
Get a single country by ID.

**Response `data`**
```json
{
  "id": 1,
  "name": "India",
  "code": "IND",
  "isActive": true,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### GET `/country`
Get paginated list of countries.

**Query Parameters**

| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number |
| `pageSize` | int | 10 | Items per page |
| `search` | string | — | Filter by name or code |

---

## State Module

All endpoints require auth.

### POST `/state`
Create a state.

**Request**
```json
{
  "name": "Maharashtra",
  "code": "MH",
  "countryId": 1
}
```

---

### PUT `/state/{id}`
Update a state.

**Request**
```json
{
  "name": "Maharashtra",
  "code": "MH",
  "isActive": true
}
```

---

### DELETE `/state/{id}`
Soft-delete a state.

---

### GET `/state/{id}`
Get a state by ID.

**Response `data`**
```json
{
  "id": 1,
  "name": "Maharashtra",
  "code": "MH",
  "countryId": 1,
  "countryName": "India",
  "isActive": true,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### GET `/state`
Get paginated list of states.

**Query Parameters** — same as `/country`.

---

### GET `/state/by-country/{countryId}`
Get all active states for a given country (no pagination).

---

## City Module

All endpoints require auth.

### POST `/city`
Create a city.

**Request**
```json
{
  "name": "Mumbai",
  "stateId": 1
}
```

---

### PUT `/city/{id}`
Update a city.

**Request**
```json
{
  "name": "Mumbai",
  "isActive": true
}
```

---

### DELETE `/city/{id}`
Soft-delete a city.

---

### GET `/city/{id}`
Get a city by ID.

**Response `data`**
```json
{
  "id": 1,
  "name": "Mumbai",
  "stateId": 1,
  "stateName": "Maharashtra",
  "countryId": 1,
  "countryName": "India",
  "isActive": true,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### GET `/city`
Get paginated list of cities.

**Query Parameters** — same as `/country`.

---

### GET `/city/by-state/{stateId}`
Get all active cities for a given state (no pagination).

---

## Encryption Module

### GET `/encryption/public-key`
Returns the server's RSA-2048 public key in PEM format.
Used by clients to encrypt the AES session key when E2E encryption is enabled.

**Response `data`**
```
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...
-----END PUBLIC KEY-----
```

---

## Organization Module

All endpoints require auth (`Bearer`).

### POST `/organization`
Create a new organization.

**Request**
```json
{ "name": "Sunrise Academy", "address": "123 Main St" }
```

---

### PUT `/organization/{id}`
Update an existing organization.

**Request**
```json
{ "name": "New Name", "address": "456 Ave", "isActive": true }
```

---

### DELETE `/organization/{id}`
Soft-delete an organization.

---

### GET `/organization/{id}`
Get an organization by ID.

**Response `data`**
```json
{ "id": 1, "name": "Sunrise Academy", "address": "123 Main St", "isActive": true, "createdAt": "..." }
```

---

### GET `/organization`
Get paginated list of organizations. Query params: `page`, `pageSize`, `search`.

---

## Menu Master Module

All endpoints require auth (`Bearer`).

### POST `/menu-master`
Create a menu.

**Request**
```json
{
  "name": "Dashboard",
  "hasChild": false,
  "parentMenuId": 0,
  "position": 1,
  "iconClass": "dashboard",
  "isUseMenuForOwnerAdmin": false
}
```

---

### PUT `/menu-master/{id}`
Update a menu.

---

### DELETE `/menu-master/{id}`
Soft-delete a menu and cascade soft-delete all child `PageMasters`, `PageMasterModules`, `PageMasterModuleActionMappings`, `MenuAndPagePermissions`.

---

### GET `/menu-master/{id}`
Get a menu by ID.

---

### GET `/menu-master`
Get paginated list of menus. Query params: `page`, `pageSize`, `search`.

---

## Page Master Module

All endpoints require auth (`Bearer`).

### POST `/page-master`
Create a page with hierarchical modules + actions. Each module entry auto-seeds `PageMasterModuleActionMappings` and `MenuAndPagePermissions` for all roles.

**Request**
```json
{
  "name": "Dashboard",
  "iconClass": "Dashboard",
  "pageUrl": "/Dashboard",
  "menuId": 1,
  "isUsePageForOwnerAdmin": false,
  "modules": [
    {
      "name": "Overview",
      "actions": [1, 2, 3, 4, 5, 6, 7]
    }
  ]
}
```

> If `actions` is omitted or empty, all 7 `ActionType` values are seeded automatically (`ADD=1, EDIT=2, DELETE=3, VIEW_LIST=4, VIEW_DETAILS=5, UPDATE_PROGRESS=6, STATUS_CHANGE=7`).

---

### PUT `/page-master/{id}`
Update a page. If `modules` is provided, new modules are upserted (existing ones skipped by name).

---

### DELETE `/page-master/{id}`
Soft-delete a page and cascade soft-delete all child modules, action mappings, and permissions.

---

### GET `/page-master/{id}`
Get a page by ID.

---

### GET `/page-master`
Get paginated list of pages. Query params: `page`, `pageSize`, `menuId` (optional filter).

---

## Menu & Page Permission Module

All endpoints require auth (`Bearer`).

### GET `/menu-and-page-permission/{id}`
Get a single permission record by ID.

---

### GET `/menu-and-page-permission`
Get paginated list of permissions. Query params: `page`, `pageSize`, `roleId`, `menuId`, `pageId`.

---

### PUT `/menu-and-page-permission/{id}/{roleId}`
Toggle the `IsAllowed` flag — **no request body needed**.
Call once → allowed (`true`). Call again → denied (`false`).
Returns `404` if `id` does not exist or `roleId` does not match the stored row.

---

## AuditLog Module

All endpoints require auth (`Bearer`). Returns paginated `AuditLog` records.

### GET `/audit-log/entity/{entityName}/{entityId}`
Get audit history for a specific record (e.g., all changes to Country with Id 5).

**Query Parameters** — same paging params as `/country` (`page`, `pageSize`, `search`).

**Response `data`** — `PagedResult<AuditLog>`
```json
{
  "items": [
    {
      "id": 1,
      "entityName": "Country",
      "entityId": "5",
      "action": "Updated",
      "oldData": "{...}",
      "newData": "{...}",
      "modifiedBy": "user-id",
      "createdBy": "admin",
      "ipAddress": "192.168.1.1",
      "location": "Localhost",
      "screenName": "Country Management",
      "tableName": "Countries",
      "batchId": "3fa85f64-...",
      "parentAuditLogId": null,
      "timestamp": "2026-03-23T07:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

### GET `/audit-log/user/{userId}`
Get all audit entries created by a specific user.

**Query Parameters** — `page`, `pageSize`.

---

### GET `/audit-log/screen/{screenName}`
Get all audit entries triggered from a specific screen (matched against `ScreenName` column).

**Example:** `/audit-log/screen/Country%20Management`

**Query Parameters** — `page`, `pageSize`.

---

### GET `/audit-log/table/{tableName}`
Get all audit entries for a specific DB table (matched against `TableName` column).

**Example:** `/audit-log/table/Countries`

**Query Parameters** — `page`, `pageSize`.

---

## Endpoint Summary

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/auth/login` | No | Authenticate user |
| POST | `/auth/register` | No | Register user |
| POST | `/auth/refresh` | No | Refresh tokens |
| POST | `/auth/logout` | Yes | Revoke refresh token |
| POST | `/auth/forgot-password` | No | Send reset email |
| POST | `/auth/reset-password` | No | Reset password |
| POST | `/country` | Yes | Create country |
| PUT | `/country/{id}` | Yes | Update country |
| DELETE | `/country/{id}` | Yes | Delete country |
| GET | `/country/{id}` | Yes | Get country |
| GET | `/country` | Yes | List countries |
| POST | `/state` | Yes | Create state |
| PUT | `/state/{id}` | Yes | Update state |
| DELETE | `/state/{id}` | Yes | Delete state |
| GET | `/state/{id}` | Yes | Get state |
| GET | `/state` | Yes | List states |
| GET | `/state/by-country/{id}` | Yes | States by country |
| POST | `/city` | Yes | Create city |
| PUT | `/city/{id}` | Yes | Update city |
| DELETE | `/city/{id}` | Yes | Delete city |
| GET | `/city/{id}` | Yes | Get city |
| GET | `/city` | Yes | List cities |
| GET | `/city/by-state/{id}` | Yes | Cities by state |
| POST | `/organization` | Yes | Create organization |
| PUT | `/organization/{id}` | Yes | Update organization |
| DELETE | `/organization/{id}` | Yes | Delete organization |
| GET | `/organization/{id}` | Yes | Get organization |
| GET | `/organization` | Yes | List organizations |
| GET | `/encryption/public-key` | No | Get RSA public key |
| POST | `/menu-master` | Yes | Create menu |
| PUT | `/menu-master/{id}` | Yes | Update menu |
| DELETE | `/menu-master/{id}` | Yes | Delete menu (cascade) |
| GET | `/menu-master/{id}` | Yes | Get menu |
| GET | `/menu-master` | Yes | List menus |
| POST | `/page-master` | Yes | Create page (hierarchical) |
| PUT | `/page-master/{id}` | Yes | Update page |
| DELETE | `/page-master/{id}` | Yes | Delete page (cascade) |
| GET | `/page-master/{id}` | Yes | Get page |
| GET | `/page-master` | Yes | List pages |
| GET | `/menu-and-page-permission/{id}` | Yes | Get permission by ID |
| GET | `/menu-and-page-permission` | Yes | List permissions (filterable) |
| PUT | `/menu-and-page-permission/{id}/{roleId}` | Yes | Toggle IsAllowed (no body) |
| GET | `/audit-log/entity/{name}/{id}` | Yes | Audit history for a record |
| GET | `/audit-log/user/{userId}` | Yes | Audit entries by user |
| GET | `/audit-log/screen/{screenName}` | Yes | Audit entries by screen |
| GET | `/audit-log/table/{tableName}` | Yes | Audit entries by table |
