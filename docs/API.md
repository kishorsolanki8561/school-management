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

## Pagination Parameters

All `GET` list endpoints accept these standard query parameters via `[FromQuery] PaginationRequest`:

| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | `1` | Page number (1-based) |
| `pageSize` | int | `20` | Items per page |
| `search` | string | — | Free-text search across name/code columns |
| `sortBy` | string | *(entity default)* | Column to sort by — whitelisted per endpoint; unrecognised values fall back to the entity default |
| `sortDescending` | bool | `false` | Sort direction: `true` = DESC, `false` = ASC |
| `status` | int | — | `1` = active records only, `0` = inactive only; omit for all. Not applicable to AuditLog or MenuAndPagePermission. |
| `dateFrom` | datetime | — | Inclusive lower bound on `CreatedAt` (or `Timestamp` for AuditLog) |
| `dateTo` | datetime | — | Inclusive upper bound on `CreatedAt` (or `Timestamp` for AuditLog) |

> `sortBy` column names are validated against a per-entity whitelist in each `*Queries.cs` — client-supplied column names are **never** injected into SQL directly.

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
| `pageSize` | int | 20 | Items per page |
| `search` | string | — | Free-text search (Name, Code where applicable) |
| `sortBy` | string | `Name` | Column to sort by. Allowed: `Id`, `Name`, `Code`, `IsActive`, `CreatedAt`. Falls back to default if unrecognised. |
| `sortDescending` | bool | `false` | `true` = DESC, `false` = ASC |
| `status` | int | — | `1` = active only, `0` = inactive only, omit = all |
| `dateFrom` | datetime | — | Filter records where `CreatedAt >= dateFrom` |
| `dateTo` | datetime | — | Filter records where `CreatedAt <= dateTo` |

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

**Query Parameters** — same as `/country` (`page`, `pageSize`, `search`, `sortBy`, `sortDescending`, `status`, `dateFrom`, `dateTo`).
Allowed `sortBy` values: `s.Id`, `s.Name`, `s.Code`, `s.IsActive`, `s.CreatedAt`. Default: `s.Name`.

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

**Query Parameters** — same as `/country`. Allowed `sortBy` values: `ci.Id`, `ci.Name`, `ci.IsActive`, `ci.CreatedAt`. Default: `ci.Name`.

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
Get paginated list of organizations.

**Query Parameters** — same as `/country`. Allowed `sortBy` values: `Id`, `Name`, `Address`, `IsActive`, `CreatedAt`. Default: `Name`.

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

**Response `data`**
```json
{
  "id": 1,
  "name": "Settings",
  "hasChild": true,
  "parentMenuId": null,
  "parentMenuName": null,
  "position": 2,
  "iconClass": "settings",
  "isActive": true,
  "isUseMenuForOwnerAdmin": false,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### GET `/menu-master`
Get paginated list of menus.

Query params: `page`, `pageSize`, `search`, `sortBy` (allowed: `Id`, `Name`, `HasChild`, `ParentMenuName`, `Position`, `IsActive`, `CreatedAt`; default `Position`), `sortDescending`, `status`, `dateFrom`, `dateTo`.

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
Get paginated list of pages.

Query params: `page`, `pageSize`, `menuId` (optional, filter by menu), `search`, `sortBy` (allowed: `Id`, `Name`, `PageUrl`, `IsActive`, `CreatedAt`, `MenuId`; default `Name`), `sortDescending`, `status`, `dateFrom`, `dateTo`.

---

## Menu & Page Permission Module

All endpoints require auth (`Bearer`).

### GET `/menu-and-page-permission/{id}`
Get a single permission record by ID.

---

### GET `/menu-and-page-permission`
Get paginated list of permissions.

Query params: `page`, `pageSize`, `roleId`, `menuId`, `pageId`, `sortBy` (allowed: `Id`, `MenuId`, `PageId`, `RoleId`, `ActionId`, `IsAllowed`, `CreatedAt`; default `MenuId`), `sortDescending`, `dateFrom`, `dateTo`. (`status` not applicable — no `IsActive` column.)

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

**Query Parameters** — `page`, `pageSize`, `dateFrom`, `dateTo`, `sortBy` (allowed: `Id`, `Timestamp`, `Action`, `EntityName`, `TableName`, `ScreenName`, `CreatedBy`; default `Timestamp`), `sortDescending` (default `true` when using default sort). (`status` and `search` not applicable.)

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
      "modifiedBy": "admin",
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

> `modifiedBy` is `null` when `action = "Created"`; contains the username for `"Updated"` / `"Deleted"` actions.

---

### GET `/audit-log/user/{userId}`
Get all audit entries created by a specific user.

**Query Parameters** — `page`, `pageSize`, `dateFrom`, `dateTo`, `sortBy`, `sortDescending`.

---

### GET `/audit-log/screen/{screenName}`
Get all audit entries triggered from a specific screen (matched against `ScreenName` column).

**Example:** `/audit-log/screen/Country%20Management`

**Query Parameters** — `page`, `pageSize`, `dateFrom`, `dateTo`, `sortBy`, `sortDescending`.

---

### GET `/audit-log/table/{tableName}`
Get all audit entries for a specific DB table (matched against `TableName` column).

**Example:** `/audit-log/table/Countries`

**Query Parameters** — `page`, `pageSize`, `dateFrom`, `dateTo`, `sortBy`, `sortDescending`.

---

### GET `/audit-log/hierarchy/{entityId}?entityName=&screenName=`
Get paginated audit history with full parent-child hierarchy grouped by `BatchId`.

Each page item is one DB-transaction batch. Root nodes contain child records
(e.g. `PageMasterModules` nested under `PageMaster`) linked via `ParentAuditLogId`.

| Parameter | Type | Required | Description |
|---|---|---|---|
| `entityId` | string | **Yes** | The entity record ID to look up |
| `entityName` | string | No | Optional: narrow to a specific entity type (e.g. `"Country"`). Blank or whitespace treated as omitted. |
| `screenName` | string | No | Optional: narrow to entries from a specific screen. Blank or whitespace treated as omitted. |
| `page` | int | No | Page number (default `1`) — paging is over **batches**, not individual rows |
| `pageSize` | int | No | Batches per page (default `20`) |

**Response `data`** — `PagedResult<AuditLogBatchResponse>`
```json
{
  "items": [
    {
      "batchId": "3fa85f64...",
      "timestamp": "2026-03-28T09:00:00Z",
      "screenName": "Country Management",
      "createdBy": "admin",
      "modifiedBy": null,
      "ipAddress": "192.168.1.1",
      "location": "Localhost",
      "entries": [
        {
          "id": 1,
          "entityName": "Country",
          "entityId": "5",
          "action": "Created",
          "oldData": null,
          "newData": "{...}",
          "modifiedBy": null,
          "createdBy": "admin",
          "screenName": "Country Management",
          "tableName": "Countries",
          "timestamp": "2026-03-28T09:00:00Z",
          "ipAddress": "192.168.1.1",
          "location": "Localhost",
          "children": []
        }
      ]
    }
  ],
  "totalCount": 3,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

## Org File Upload Config Module

Manages per-screen file upload validation rules per organisation. All endpoints require auth (`Bearer`).

> **OwnerAdmin** always bypasses org config and uses `FileUploadDefaults` from `appsettings.json`.

### POST `/org-file-upload-config`
Create a file upload config for a specific org + screen combination.

**Request**
```json
{
  "orgId": 1,
  "pageId": 3,
  "allowedExtensions": ".pdf,.jpg,.png",
  "allowedMimeTypes": "application/pdf,image/jpeg,image/png",
  "maxFileSizeBytes": 5242880,
  "allowMultiple": false
}
```

**Response `data`** — `OrgFileUploadConfigResponse`
```json
{
  "id": 1,
  "orgId": 1,
  "pageId": 3,
  "allowedExtensions": ".pdf,.jpg,.png",
  "allowedMimeTypes": "application/pdf,image/jpeg,image/png",
  "maxFileSizeBytes": 5242880,
  "allowMultiple": false,
  "isActive": true,
  "createdAt": "2026-03-27T00:00:00Z"
}
```

Returns `409 Conflict` if a config for `(orgId, pageId)` already exists.

---

### PUT `/org-file-upload-config/{id}`
Update an existing file upload config.

**Request**
```json
{
  "allowedExtensions": ".pdf,.docx",
  "allowedMimeTypes": "application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "maxFileSizeBytes": 10485760,
  "allowMultiple": true,
  "isActive": true
}
```

---

### GET `/org-file-upload-config/{id}`
Get a file upload config by ID.

---

### GET `/org-file-upload-config/screen?orgId={int}&pageId={int}`
Get the active file upload config for a specific org and screen (used at upload time to determine validation rules).

Returns `404` if no active config exists for the `(orgId, pageId)` pair.

---

## File Upload Module

All endpoints require auth (`Bearer`).

### POST `/file-upload?pageId={int?}&orgId={int?}`
Upload one or more files. Both query parameters are optional.

**Content-Type:** `multipart/form-data`

**Form Fields**

| Field | Type | Required | Description |
|---|---|---|---|
| `files` | `IFormFile[]` | Yes | One or more files |
| `pageId` | int (query) | No | Screen page ID — used to resolve folder name and org config |
| `orgId` | int (query) | No | Organisation ID — used to resolve folder name and org config |

**Folder Resolution Logic**

| orgId | pageId | Upload folder |
|---|---|---|
| Provided | Provided | `{OrgName}/{PageName}` |
| null | Provided | `{PageName}` |
| Provided | null | `{OrgName}` |
| null | null | `AllAttachment` |

**Config Resolution Logic**

| Caller role | orgId | pageId | Config used |
|---|---|---|---|
| `OwnerAdmin` | any | any | `appsettings.json` defaults |
| Other | Provided | Provided | `OrgFileUploadConfig` for `(orgId, pageId)` (falls back to defaults if none found) |
| Other | null or missing | — | `appsettings.json` defaults |

**Response `data`** — `IList<FileUploadResponse>`
```json
[
  {
    "fileName": "invoice.pdf",
    "filePath": "/uploads/Sunrise Academy/Admissions/a1b2c3d4.pdf",
    "sizeBytes": 4096,
    "contentType": "application/pdf"
  }
]
```

Returns `400 Bad Request` if:
- A file fails extension / MIME type / size validation
- Multiple files are submitted but `allowMultiple = false`

---

## Dropdown Module

All endpoints require auth (`Bearer`).

Generic endpoint that returns dropdown data for any registered key. Always returns `name` / `value` pairs; optional extra columns are appended as camelCase keys.

### POST `/dropdown`
Fetch dropdown items for the specified key.

**Request**
```json
{
  "key": "StateDDL",
  "extraColumns": ["CountryId"],
  "filters": {
    "CountryId": 3
  }
}
```

> `key` is a **string enum** — case-insensitive. Supported values listed below.

**Supported Keys**

| Key | Source Table | Allowed Extra Columns | Allowed Filter Columns |
|---|---|---|---|
| `CountryDDL` | Countries | `Code` | `IsActive` |
| `StateDDL` | States | `Code`, `CountryId` | `CountryId`, `IsActive` |
| `CityDDL` | Cities | `StateId` | `StateId`, `IsActive` |
| `OrganizationDDL` | Organizations | `Address` | `IsActive` |
| `RolesDDL` | Roles | `Description`, `IsOrgRole` | `IsOrgRole` |
| `ParentMenuDDL` | MenuMasters (`HasChild=1` only) | `Position`, `IconClass` | `IsActive` |
| `MenuDDL` | MenuMasters | `ParentMenuId`, `Position`, `IconClass`, `HasChild` | `ParentMenuId`, `IsActive`, `HasChild` |
| `PageDDL` | PageMasters | `MenuId`, `PageUrl`, `IconClass` | `MenuId`, `IsActive` |
| `SchoolDDL` | Organizations (approved only) | `SchoolCode`, `Address` | `IsActive`, `IsApproved` |

**Response `data`** — `IEnumerable<Dictionary<string, object?>>`
```json
[
  { "name": "Maharashtra", "value": 1, "countryId": 1 },
  { "name": "Gujarat",     "value": 2, "countryId": 1 }
]
```

> Extra column names are always returned **camelCased** (`CountryId` → `countryId`).

Returns `400 Bad Request` if:
- `key` is not a registered `DropdownKey` value
- `extraColumns` contains a column not in the whitelist for that key
- `filters` contains a key not in the whitelist for that key

---

## Org-Scoped Role Permissions

When a school is approved, the system **automatically copies** all 29 system roles and their `MenuAndPagePermissions` into org-specific rows tagged with that school's `OrgId`.

- **OwnerAdmin** — bypasses all permission checks, always sees everything
- **All other roles (SuperAdmin, Admin, Teacher, etc.)** — resolved from org-specific permission copies
- **SuperAdmin and Admin** of an org can toggle (`IsAllowed`) any permission within their own org via `PUT /menu-and-page-permission/org/{orgId}/{id}/{roleId}`
- Login (`/auth/login`) and switch-school (`/auth/switch-school`) automatically return menus/pages filtered by the user's org permissions

---

## School Management

### POST `/school/register`
Registers a new school. Creates an `Organization` record (inactive, unapproved) and a `SchoolApprovalRequest` (Pending).

**Request body**
```json
{ "name": "Sunrise Academy", "address": "123 Main St" }
```

### PUT `/school/{id}/approve`
OwnerAdmin approves the school. Sets `Organization.IsApproved = true` and stamps `ApprovedAt`/`ApprovedBy`.

### PUT `/school/{id}/reject`
OwnerAdmin rejects a pending request with a reason.

**Request body**
```json
{ "reason": "Incomplete documentation" }
```

### GET `/school/pending-approvals`
Paginated list of all `Pending` approval requests. OwnerAdmin only.

### GET `/school/{id}/approval-history`
Full approval history for a school (all `SchoolApprovalRequest` rows for that org).

---

## User Management

### POST `/user-management`
Creates a new user scoped to the caller's org. Caller must have an active `OrgId` in their JWT.

**Request body**
```json
{
  "username": "john.doe",
  "email": "john@school.com",
  "password": "Secret123!",
  "roleIds": [3]
}
```

### POST `/user-management/{id}/roles`
Assigns a role to a user within the caller's org.

**Request body**
```json
{ "roleId": 4 }
```

### DELETE `/user-management/{id}/roles/{roleId}`
Removes a role assignment within the caller's org (soft-delete).

### PUT `/user-management/{id}/role-level`
OwnerAdmin only. Upgrades or downgrades a user's system-level role between `SuperAdmin` and `Admin`.

**Request body**
```json
{ "targetRole": "SuperAdmin" }
```

Valid values: `"SuperAdmin"`, `"Admin"`. OwnerAdmin accounts cannot be modified via this endpoint.

---

## Switch School (Auth)

### POST `/auth/switch-school`
Switches the caller's active school context and returns a fresh token pair with the new `OrgId` claim embedded.

**Request body**
```json
{ "orgId": 2 }
```

The caller must be a member of the target org (via `UserOrganizationMappings`). Returns the same `LoginResponse` shape as `/auth/login`.

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
| GET | `/audit-log/entity/{name}/{id}` | Yes | Audit history for a record (flat) |
| GET | `/audit-log/user/{userId}` | Yes | Audit entries by user |
| GET | `/audit-log/screen/{screenName}` | Yes | Audit entries by screen |
| GET | `/audit-log/table/{tableName}` | Yes | Audit entries by table |
| GET | `/audit-log/hierarchy/{entityId}` | Yes | Audit hierarchy by EntityId (+ optional entityName, screenName) |
| POST | `/org-file-upload-config` | Yes | Create org file upload config |
| PUT | `/org-file-upload-config/{id}` | Yes | Update org file upload config |
| GET | `/org-file-upload-config/{id}` | Yes | Get config by ID |
| GET | `/org-file-upload-config/screen` | Yes | Get config by orgId + pageId |
| POST | `/file-upload` | Yes | Upload files (multipart/form-data) |
| POST | `/dropdown` | Yes | Get dropdown data by key |
| POST | `/school/register` | Yes | Register a new school (creates approval request) |
| PUT | `/school/{id}` | Yes | Update school info |
| DELETE | `/school/{id}` | Yes | Soft-delete school (OwnerAdmin) |
| GET | `/school/{id}` | Yes | Get school by ID |
| GET | `/school` | Yes | List schools (OwnerAdmin sees all) |
| PUT | `/school/{id}/approve` | Yes | Approve school (OwnerAdmin) |
| PUT | `/school/{id}/reject` | Yes | Reject school with reason (OwnerAdmin) |
| GET | `/school/pending-approvals` | Yes | List pending approval requests (OwnerAdmin) |
| GET | `/school/{id}/approval-history` | Yes | Approval history for a school |
| POST | `/user-management` | Yes | Create user in caller's org |
| PUT | `/user-management/{id}` | Yes | Update user info |
| DELETE | `/user-management/{id}` | Yes | Soft-delete user |
| GET | `/user-management/{id}` | Yes | Get user by ID with roles |
| GET | `/user-management` | Yes | List users (org-scoped or all for OwnerAdmin) |
| POST | `/user-management/{id}/roles` | Yes | Assign role to user |
| DELETE | `/user-management/{id}/roles/{roleId}` | Yes | Remove role from user |
| PUT | `/user-management/{id}/role-level` | Yes | Upgrade/downgrade between SuperAdmin ↔ Admin (OwnerAdmin) |
| POST | `/auth/switch-school` | Yes | Switch active school context, returns new token pair |
| PUT | `/menu-and-page-permission/org/{orgId}/{id}/{roleId}` | Yes | Toggle org-specific permission (SuperAdmin/Admin) |
| GET | `/menu-and-page-permission/org/{orgId}` | Yes | Get org-specific permissions (filterable by menuId, pageId, roleId) |
| PUT | `/org-storage-config` | Yes | Create/update file storage config for caller's org |
| GET | `/org-storage-config/{orgId}` | Yes | Get file storage config for an org |
| DELETE | `/org-storage-config/{orgId}` | Yes | Soft-delete file storage config for an org |
| PUT | `/org-notification-config` | Yes | Create/update channel config for caller's org |
| GET | `/org-notification-config/{orgId}` | Yes | Get all channel configs for an org |
| GET | `/org-notification-config/{orgId}/{channel}` | Yes | Get config for a specific channel |
| DELETE | `/org-notification-config/{orgId}/{channel}` | Yes | Delete a channel config |
| PUT | `/notification-template` | Yes | Create/update notification template (OwnerAdmin = global) |
| GET | `/notification-template/{orgId}` | Yes | List all templates for an org (orgId=0 = global defaults) |
| GET | `/notification-template/{orgId}/{eventType}/{channel}` | Yes | Get template by event + channel |
| DELETE | `/notification-template/{id}` | Yes | Soft-delete a template |
| GET | `/in-app-notification` | Yes | Get current user's in-app notifications (paginated) |
| GET | `/in-app-notification/unread-count` | Yes | Get unread notification count |
| PUT | `/in-app-notification/mark-read` | Yes | Mark specific notifications as read |
| PUT | `/in-app-notification/mark-all-read` | Yes | Mark all notifications as read |
| WS  | `/hubs/notifications` | Yes | SignalR hub — receive real-time in-app notifications |

---

## Org Storage Config

Each org configures **one** active file storage backend. Storage type determines which fields are required.

### PUT `/org-storage-config`
Create or update the storage config for the caller's org (upsert by OrgId).

**Storage types:** `HostingServer` | `AWSS3` | `AzureBlob`

**Request body — HostingServer**
```json
{
  "storageType": "HostingServer",
  "basePath": "/var/uploads/schools"
}
```

**Request body — AWS S3**
```json
{
  "storageType": "AWSS3",
  "bucketName": "my-school-bucket",
  "region": "ap-south-1",
  "accessKey": "AKIA...",
  "secretKey": "..."
}
```

**Request body — Azure Blob**
```json
{
  "storageType": "AzureBlob",
  "containerName": "school-uploads",
  "connectionString": "DefaultEndpointsProtocol=https;AccountName=..."
}
```

> **Note:** `SecretKey` and `ConnectionString` are write-only — they are never returned in GET responses.

---

---

## Unified Notification System

The notification system supports multiple channels per event. Channels are dispatched in parallel.

### Channels
| Channel | Description |
|---|---|
| `Email` | SMTP email via org config or appsettings fallback |
| `SMS` | SMS via Twilio / Infobip / SslWireless (org picks provider) |
| `Push` | Firebase FCM — future update |
| `InApp` | Stored in DB + real-time via SignalR |

### SMS Providers
| Provider | Owner Admin Default | Notes |
|---|---|---|
| `Infobip` | ✅ appsettings | International |
| `Twilio` | — | Org configures own credentials |
| `SslWireless` | — | BD local provider |

### Template Resolution Order (per channel)
1. Org-specific + channel-specific  (`OrgId=X, Channel=Email`)
2. Org-specific + generic fallback  (`OrgId=X, Channel=null`)
3. Global + channel-specific        (`OrgId=null, Channel=Email`)
4. Global + generic fallback        (`OrgId=null, Channel=null`)
5. Not found → warning in result

### Supported Placeholders
| Placeholder | Description |
|---|---|
| `{{SchoolName}}` | Name of the school/org |
| `{{AdminName}}` | Name of the admin user |
| `{{Date}}` | Current date (UTC, yyyy-MM-dd) |
| `{{RejectionReason}}` | Reason for rejection |
| `{{StudentName}}` | Student name |
| `{{FeeDueDate}}` | Fee due date |
| `{{Amount}}` | Fee amount |

### PUT `/org-notification-config`
Create or update a channel config for the caller's org (upsert by OrgId + Channel).

**Email channel request body**
```json
{
  "channel": "Email",
  "smtpHost": "smtp.gmail.com",
  "smtpPort": 587,
  "smtpUsername": "noreply@school.com",
  "smtpPassword": "...",
  "fromAddress": "noreply@school.com",
  "fromName": "My School",
  "enableSsl": true
}
```

**SMS channel (Twilio) request body**
```json
{
  "channel": "SMS",
  "smsProvider": "Twilio",
  "accountSid": "ACxxx",
  "authToken": "...",
  "senderNumber": "+1234567890"
}
```

**SMS channel (Infobip) request body**
```json
{
  "channel": "SMS",
  "smsProvider": "Infobip",
  "apiKey": "...",
  "senderName": "MySchool"
}
```

> Secrets (password, authToken, apiKey, smtpPassword, pushServerKey) are write-only — not returned in GET responses.

### PUT `/notification-template`
Create or update a notification template. OwnerAdmin creates global defaults (OrgId = null); org users create org-specific templates.

**Request body**
```json
{
  "eventType": "SchoolApproved",
  "channel": "Email",
  "subject": "Your school {{SchoolName}} is approved!",
  "body": "<p>Dear {{AdminName}},</p><p>Approved on {{Date}}.</p>",
  "isBodyHtml": true,
  "toAddresses": null,
  "ccAddresses": "support@platform.com",
  "bccAddresses": null
}
```

Set `channel: null` for a generic template used as fallback for all channels.

### SignalR — `/hubs/notifications`
Connect with a valid JWT. The server pushes `ReceiveNotification` events:
```json
{
  "id": 42,
  "eventType": "SchoolApproved",
  "title": "Your school has been approved!",
  "body": "...",
  "createdAt": "2026-03-31T20:00:00Z"
}
```
