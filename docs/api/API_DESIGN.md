# RESTful API & SignalR Specification (API_DESIGN.md)

> **Hệ thống:** Restaurant POS & KDS System  
> **Phiên bản API:** v1  
> **Kiến trúc:** RESTful API & SignalR Realtime Communication  
> **Thời gian cập nhật:** 2026-07-23  

---

## 📌 Part 1: General Standards & Conventions

### 1. Base URL
Tất cả các RESTful API endpoints trong hệ thống đều tuân theo chuẩn tiền tố URL:
```http
https://{domain_or_host}/api/v1
```

---

### 2. Authentication & Authorization Header
Hệ thống sử dụng cơ chế xác thực **JSON Web Token (JWT)** hoặc **Cookie Session** đối với các API nội bộ:
- **HTTP Header:**
  ```http
  Authorization: Bearer <JWT_TOKEN>
  ```
- **JWT Claims Payload:** Chứa `sub` (UserId), `unique_name` (Username), `role` (RoleName).
- **Guest Access (QR Ordering):** Khách hàng chọn món qua QR Code không bắt buộc đăng nhập tài khoản. API QR sử dụng `tableId` / `token` query parameters để định danh phiên bàn ăn (`TableGroup_{TableId}`).

---

### 3. Standard Response Format
Tất cả các API response (HTTP 200, 201, 400, 401, 403, 404, 409, 429, 500) đều chuẩn hóa định dạng JSON bao gồm 4 trường thông tin cốt lõi:

#### 3.1. Success Response Structure (`HTTP 200 OK` / `HTTP 201 Created`)
```json
{
  "success": true,
  "data": { ... },
  "error": null,
  "timestamp": "2026-07-23T00:00:00Z"
}
```

#### 3.2. Error Response Structure (`HTTP 400` / `401` / `403` / `404` / `409` / `429` / `500`)
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "BAD_REQUEST",
    "message": "Thông tin dữ liệu gửi lên không hợp lệ.",
    "details": [
      "Trường 'Name' không được để trống."
    ]
  },
  "timestamp": "2026-07-23T00:00:00Z"
}
```

---

### 4. HTTP Status Codes Specification

| Code | Status | Ý nghĩa & Trường hợp áp dụng |
| :---: | :--- | :--- |
| **`200`** | `OK` | Xử lý request thành công và trả về dữ liệu (GET, PUT, PATCH). |
| **`201`** | `Created` | Tạo mới tài nguyên thành công (POST). Kèm header `Location` đến tài nguyên vừa tạo. |
| **`204`** | `No Content` | Xóa thành công hoặc xử lý thành công nhưng không cần trả về data trong Body (DELETE). |
| **`400`** | `Bad Request` | Lỗi dữ liệu đầu vào (Validation fail, sai logic nghiệp vụ, thiếu tham số). |
| **`401`** | `Unauthorized` | Unauthenticated - Chưa đăng nhập hoặc JWT Token đã hết hạn / không hợp lệ. |
| **`403`** | `Forbidden` | Access Denied - Đã đăng nhập nhưng không có quyền hạn (Role restriction, e.g. Chef gọi API Admin). |
| **`404`** | `Not Found` | Không tìm thấy tài nguyên yêu cầu (Entity ID không tồn tại trên DB). |
| **`409`** | `Conflict` | Xung đột dữ liệu (Concurrency conflict `RowVersion`, trùng lặp dữ liệu unique). |
| **`429`** | `Too Many Requests` | Rate Limiting - Khách bấm gửi đơn liên tục (< 30 giây giữa 2 lần gửi). |
| **`500`** | `Internal Server Error` | Lỗi hệ thống không xác định tại backend. |

---

## 📌 Part 2: RESTful API Endpoints Detail

### 1. Authentication Module (`/api/v1/auth`)

#### 1.1. Đăng nhập hệ thống
- **HTTP Method & Path:** `POST /api/v1/auth/login`
- **Authorization / Policy:** `Public` (`AllowAnonymous`)
- **Description:** Xác thực nhân viên bằng tên đăng nhập và mật khẩu, trả về JWT Token và thông tin vai trò.
- **Request Body:**
  | Field | Type | Required | Description |
  | :--- | :--- | :---: | :--- |
  | `username` | `string` | Yes | Tên tài khoản nhân viên |
  | `password` | `string` | Yes | Mật khẩu tài khoản |

- **Response Examples:**
  - **`200 OK` (Thành công):**
    ```json
    {
      "success": true,
      "data": {
        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "username": "admin",
        "fullName": "System Administrator",
        "roleName": "Admin",
        "expiresAt": "2026-07-23T08:00:00Z"
      },
      "error": null,
      "timestamp": "2026-07-23T00:00:00Z"
    }
    ```
  - **`401 Unauthorized` (Sai tài khoản/mật khẩu):**
    ```json
    {
      "success": false,
      "data": null,
      "error": {
        "code": "UNAUTHORIZED",
        "message": "Tên đăng nhập hoặc mật khẩu không chính xác."
      },
      "timestamp": "2026-07-23T00:00:00Z"
    }
    ```

#### 1.2. Đăng xuất hệ thống
- **HTTP Method & Path:** `POST /api/v1/auth/logout`
- **Authorization / Policy:** `Authorize` (Tất cả nhân viên đã đăng nhập)
- **Description:** Đăng xuất tài khoản khỏi hệ thống (Stateless JWT logout).
- **Response Examples:**
  - **`200 OK`:**
    ```json
    {
      "success": true,
      "data": {
        "message": "Đăng xuất thành công."
      },
      "error": null,
      "timestamp": "2026-07-23T00:00:00Z"
    }
    ```

#### 1.3. Lấy thông tin tài khoản hiện tại
- **HTTP Method & Path:** `GET /api/v1/auth/me`
- **Authorization / Policy:** `Authorize`
- **Description:** Trả về thông tin hồ sơ chi tiết của nhân viên đang đăng nhập dựa trên JWT Token.
- **Response Examples:**
  - **`200 OK`:**
    ```json
    {
      "success": true,
      "data": {
        "id": 1,
        "username": "cashier01",
        "fullName": "Nguyễn Văn Thu Ngân",
        "roleName": "Cashier",
        "email": "cashier@restaurant.com",
        "phoneNumber": "0901234567",
        "isActive": true
      },
      "error": null,
      "timestamp": "2026-07-23T00:00:00Z"
    }
    ```

---

### 2. Categories Module (`/api/v1/categories`)

#### 2.1. Lấy danh sách tất cả danh mục
- **HTTP Method & Path:** `GET /api/v1/categories`
- **Authorization / Policy:** `Public` (`AllowAnonymous`)
- **Description:** Lấy toàn bộ danh sách danh mục thực đơn (phục vụ cả BOH, POS và khách quét QR).

#### 2.2. Lấy chi tiết danh mục theo ID
- **HTTP Method & Path:** `GET /api/v1/categories/{id}`
- **Authorization / Policy:** `Public` (`AllowAnonymous`)

#### 2.3. Tạo danh mục mới
- **HTTP Method & Path:** `POST /api/v1/categories`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager")`
- **Request Body:**
  | Field | Type | Required | Description |
  | :--- | :--- | :---: | :--- |
  | `name` | `string` | Yes | Tên danh mục (ví dụ: "Khai vị", "Món chính") |
  | `description` | `string` | No | Mô tả ngắn về danh mục |
  | `imageUrl` | `string` | No | Đường dẫn hình ảnh minh họa |
  | `sortOrder` | `integer` | Yes | Thứ tự sắp xếp hiển thị |

#### 2.4. Cập nhật danh mục
- **HTTP Method & Path:** `PUT /api/v1/categories/{id}`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager")`

#### 2.5. Xóa danh mục (Soft Delete)
- **HTTP Method & Path:** `DELETE /api/v1/categories/{id}`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager")`
- **Response Examples:**
  - **`204 No Content`:** Xóa thành công.
  - **`404 Not Found`:** `Không tìm thấy danh mục.`

---

### 3. MenuItems Module (`/api/v1/menu-items`)

#### 3.1. Lấy tất cả món ăn (Admin/Staff view)
- **HTTP Method & Path:** `GET /api/v1/menu-items`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager,Cashier,Chef")`

#### 3.2. Lấy danh sách món ăn đang hoạt động (Customer/QR view)
- **HTTP Method & Path:** `GET /api/v1/menu-items/active`
- **Authorization / Policy:** `Public` (`AllowAnonymous`)

#### 3.3. Lấy chi tiết món ăn kèm Modifier Groups
- **HTTP Method & Path:** `GET /api/v1/menu-items/{id}`
- **Authorization / Policy:** `Public` (`AllowAnonymous`)

#### 3.4. Tạo món ăn mới
- **HTTP Method & Path:** `POST /api/v1/menu-items`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager")`
- **Request Body:**
  | Field | Type | Required | Description |
  | :--- | :--- | :---: | :--- |
  | `categoryId` | `integer` | Yes | ID danh mục thuộc về |
  | `name` | `string` | Yes | Tên món ăn |
  | `description` | `string` | No | Mô tả chi tiết món |
  | `price` | `decimal` | Yes | Đơn giá món ăn |
  | `imageUrl` | `string` | No | Ảnh đại diện món |
  | `isCombo` | `boolean` | Yes | Cờ đánh dấu món Combo |
  | `sortOrder` | `integer` | Yes | Thứ tự hiển thị |
  | `modifierGroupIds`| `array[int]`| No | Danh sách ID nhóm tùy chọn đi kèm |

#### 3.5. Cập nhật thông tin món ăn
- **HTTP Method & Path:** `PUT /api/v1/menu-items/{id}`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager")`

#### 3.6. Xóa món ăn
- **HTTP Method & Path:** `DELETE /api/v1/menu-items/{id}`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager")`

#### 3.7. Toggle báo hết món (Sold Out)
- **HTTP Method & Path:** `PATCH /api/v1/menu-items/{id}/toggle-sold-out`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager,Chef,Cashier")`
- **Description:** Bật/tắt trạng thái hết món nhanh từ KDS hoặc POS. Tự động broadcast SignalR `ReceiveSoldOutNotice`.

---

### 4. Modifiers Module (`/api/v1/modifiers`)

#### 4.1. Lấy tất cả Modifier Groups
- **HTTP Method & Path:** `GET /api/v1/modifiers/groups`
- **Authorization / Policy:** `Public` (`AllowAnonymous`)

#### 4.2. Tạo nhóm Modifier mới
- **HTTP Method & Path:** `POST /api/v1/modifiers/groups`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager")`
- **Request Body:**
  | Field | Type | Required | Description |
  | :--- | :--- | :---: | :--- |
  | `name` | `string` | Yes | Tên nhóm (e.g. "Size", "Topping", "Món phụ Combo") |
  | `isRequired` | `boolean` | Yes | Bắt buộc chọn hay tùy chọn |
  | `minSelections` | `integer` | Yes | Số lượng chọn tối thiểu |
  | `maxSelections` | `integer` | Yes | Số lượng chọn tối đa |
  | `sortOrder` | `integer` | Yes | Thứ tự hiển thị |
  | `options` | `array` | No | Danh sách các tùy chọn khởi tạo kèm theo |

#### 4.3. Thêm tùy chọn (Option) vào nhóm Modifier
- **HTTP Method & Path:** `POST /api/v1/modifiers/groups/{groupId}/options`
- **Authorization / Policy:** `Authorize(Roles = "Admin,Manager")`

#### 4.4. Cập nhật/Xóa Modifier Options
- `PUT /api/v1/modifiers/options/{optionId}` — `Authorize(Roles = "Admin,Manager")`
- `DELETE /api/v1/modifiers/options/{optionId}` — `Authorize(Roles = "Admin,Manager")`

---

### 5. Areas & Tables Module (`/api/v1/areas`, `/api/v1/tables`)

#### 5.1. Lấy danh sách Khu vực (Areas) & Bàn ăn (Tables)
- `GET /api/v1/areas` — `Public`
- `GET /api/v1/tables` — `Public`
- `GET /api/v1/tables/area/{areaId}` — `Public`

#### 5.2. Quản lý Bàn ăn (Create/Update/Delete)
- `POST /api/v1/tables` — `Authorize(Roles = "Admin,Manager")`
- `PUT /api/v1/tables/{id}` — `Authorize(Roles = "Admin,Manager")`
- `DELETE /api/v1/tables/{id}` — `Authorize(Roles = "Admin,Manager")`

#### 5.3. Sinh & Tải ảnh mã QR Code của Bàn
- **HTTP Method & Path:** `GET /api/v1/tables/{id}/qr-code`
- **Authorization / Policy:** `Public` (`AllowAnonymous`)
- **Description:** Sinh mã QR PNG chứa link truy cập đặt món bảo mật kèm token duy nhất cho bàn.
- **Response Format:** Binary Image (`image/png`).

---

### 6. Kitchen Display System (KDS) Module (`/api/v1/kds`)

#### 6.1. Lấy danh sách Ticket bếp đang hoạt động (Active Tickets)
- **HTTP Method & Path:** `GET /api/v1/kds/tickets/active`
- **Authorization / Policy:** `Public` (`AllowAnonymous`)
- **Description:** Lấy danh sách các thẻ đơn hàng đang chờ làm (Pending) hoặc đang làm (Processing) tại bếp.

#### 6.2. Cập nhật trạng thái làm món của Kitchen Item
- **HTTP Method & Path:** `PATCH /api/v1/kds/items/{id}/status`
- **Authorization / Policy:** `Authorize(Roles = "Chef,Manager,Admin")`
- **Request Body:**
  | Field | Type | Required | Description |
  | :--- | :--- | :---: | :--- |
  | `kitchenStatus` | `integer` | Yes | `0`: Pending, `1`: Processing, `2`: Done |

- **Response Example (`200 OK`):**
  ```json
  {
    "success": true,
    "data": {
      "orderItemId": 105,
      "orderId": 42,
      "menuItemId": 12,
      "menuItemName": "Phở Bò Đặc Biệt",
      "quantity": 2,
      "kitchenStatus": 1,
      "kitchenStatusName": "Processing",
      "sentToKitchenAt": "2026-07-23T00:10:00Z",
      "waitingMinutes": 13,
      "isSlaWarning": true
    },
    "error": null,
    "timestamp": "2026-07-23T00:23:00Z"
  }
  ```

#### 6.3. Báo hết món trực tiếp từ Bếp
- **HTTP Method & Path:** `POST /api/v1/kds/items/{menuItemId}/sold-out`
- **Authorization / Policy:** `Authorize(Roles = "Chef,Manager,Admin")`

---

## 📌 Part 3: Realtime Communication (SignalR Hub)

### 1. Hub URL & Protocol
- **Hub Endpoint:** `/hubs/restaurant`
- **Transport Protocols:** WebSockets (Ưu tiên), Server-Sent Events, Long Polling.

---

### 2. Client Connection & Groups Management
Khi client thiết lập kết nối tới Hub, hệ thống đọc vai trò nhân viên từ JWT Claims (hoặc đọc query string `tableId`) để tự động sắp xếp connection vào các Groups:

```mermaid
graph TD
    Client[SignalR Client Connection] --> AuthCheck{Authentication & Context}
    AuthCheck -->|Role: Admin / Manager / Cashier| GroupCashiers[CashiersGroup]
    AuthCheck -->|Role: Admin / Chef| GroupChefs[ChefsGroup]
    AuthCheck -->|Query String: tableId > 0| GroupTable[TableGroup_{TableId}]
```

1. **`CashiersGroup`:** Nhận cập nhật sơ đồ bàn, đơn QR mới gửi, cảnh báo thanh toán, cảnh báo trễ món KDS SLA.
2. **`ChefsGroup`:** Nhận tickets đơn hàng mới gửi xuống bếp và các thông báo báo hết món.
3. **`TableGroup_{TableId}`:** Nhận cập nhật trạng thái món ăn của riêng bàn đó, sự kiện cưỡng bức chuyển/gộp bàn (`TableSessionMoved`).

Client có thể chủ động invoke hàm `JoinTableGroup(int tableId)` trên Hub khi cần join nhóm bàn thủ công.

---

### 3. Server-to-Client Events Specification (`IRestaurantHubClient`)

| Event Name | Recipient Group | Payload Structure | Mô tả sự kiện |
| :--- | :--- | :--- | :--- |
| **`ReceiveKitchenTicket`** | `ChefsGroup` | `KitchenTicketDto` | Đẩy ticket đơn hàng mới xuống bếp chế biến khi Thu ngân confirm đơn. |
| **`ReceiveItemStatusUpdate`** | `CashiersGroup`, `TableGroup_{TableId}` | `orderItemId` (int), `status` (string), `orderId` (int) | Báo trạng thái làm món đổi (`Pending` $\rightarrow$ `Processing` $\rightarrow$ `Done`). |
| **`ReceiveKitchenSlaWarning`** | `CashiersGroup` | `orderItemId` (int), `pendingMinutes` (int) | Cảnh báo trễ món SLA (> 15 phút chưa hoàn thành chế biến). |
| **`ReceiveSoldOutNotice`** | All Clients | `menuItemId` (int), `isSoldOut` (bool) | Thông báo tức thời món ăn đã báo hết hàng / mở bán lại. |
| **`ReceiveTableStatusSync`** | `CashiersGroup` | `tableId` (int), `status` (string) | Đồng bộ trạng thái màu sắc bàn trên sơ đồ nhà hàng. |

---

### 4. Client-to-Server Methods

Client có thể gọi trực tiếp các phương thức công khai được định nghĩa trên Hub:

#### `JoinTableGroup(tableId)`
- **Parameters:** `tableId` (`integer`)
- **Description:** Cho phép QR Mobile Client gia nhập nhóm thông báo của bàn ăn để nhận cập nhật realtime cho đơn của bàn mình.
- **Example Call (C# SignalR Client):**
  ```csharp
  await hubConnection.SendAsync("JoinTableGroup", 5);
  ```
