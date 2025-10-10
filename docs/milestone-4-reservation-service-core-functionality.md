# Milestone 4: Reservation Service - Core Functionality

**Goal:** Implement reservation lifecycle management

**Related User Stories:** US-007 (Reserve Available Book), US-008 (View Active Reservations), US-009 (Checkout Book), US-010 (Return Book), US-011 (View Borrowing History)

---

## Business Requirements

### Reservation Creation (US-007)
- Patrons can reserve books that have available copies
- Users are limited to 5 active reservations (Reserved or CheckedOut status)
- Reservations expire after 7 days if not picked up
- When a book is reserved:
   - Available copies count decreases by 1
   - Reservation status is set to Reserved
   - Expiration date is set to 7 days from reservation
- Error conditions:
   - Attempting to reserve when at 5 active reservations
   - Attempting to reserve a book with no available copies

### Active Reservations View (US-008)
- Patrons can view all their active reservations (Reserved or CheckedOut)
- For Reserved books: show days until pickup deadline expires
- For CheckedOut books: show days until due date
- Display book information (title and author) with each reservation
- Show total count of active reservations

### Checkout Process (US-009)
- Only Librarian role can process checkouts
- Can only checkout reservations with Reserved status
- Checkout period is 14 days from checkout date
- Optional notes can be recorded about book condition at checkout
- Returns formatted message with due date

### Return Processing (US-010)
- Only Librarian role can process returns
- Can only return reservations with CheckedOut status
- Book condition must be recorded (Good, Fair, Poor, Damaged)
- Late fees are calculated if returned after due date:
   - Rate: $1.00 per day late
- When book is returned:
   - Available copies count increases by 1
   - Reservation status is set to Returned
- Optional notes can be recorded

### Borrowing History (US-011)
- Patrons can view their complete borrowing history
- History includes all reservation statuses (Reserved, CheckedOut, Returned, Cancelled)
- Results are paginated (default: page 0, size 20)
- Sorted by most recent first
- Each record indicates if book was returned late
- Includes book information (title and author)

---

## General Technical Requirements

**Business Rules:**
- Maximum active reservations per user: 5
- Reservation expiry period: 7 days from reservation date
- Checkout period: 14 days from checkout date
- Late fee rate: $1.00 per day
- Book condition options: Good, Fair, Poor, Damaged

**Data Consistency:**
- Reservation operations must maintain data integrity
- Available copies count must stay synchronized with reservations
- Date/time calculations must be accurate and consistent

**Authorization:**
- Checkout and return operations restricted to Librarian role
- Users can only view their own reservations and history

---

## Deliverables

### 1. Reservation Creation
Implement endpoint that:
- Validates user has fewer than 5 active reservations
- Validates book has available copies
- Creates reservation with Reserved status
- Sets reservation and expiration timestamps
- Updates book's available copies count
- Returns reservation details with success message
- Handles error conditions appropriately

### 2. Active Reservations View
Implement endpoint that:
- Retrieves user's active reservations (Reserved and CheckedOut)
- Calculates time-based fields (days until expiry/due)
- Includes book information
- Returns total active count

### 3. Checkout Process
Implement endpoint that:
- Validates user has Librarian role
- Validates reservation is in Reserved status
- Updates reservation to CheckedOut status
- Records checkout timestamp
- Calculates and sets due date (14 days)
- Stores optional notes
- Returns formatted response with due date

### 4. Return Processing
Implement endpoint that:
- Validates user has Librarian role
- Validates reservation is in CheckedOut status
- Updates reservation to Returned status
- Records return timestamp and book condition
- Calculates late days and fees (if applicable)
- Updates book's available copies count
- Stores optional notes
- Returns response with late fee details if applicable

### 5. Borrowing History
Implement endpoint that:
- Retrieves user's complete borrowing history
- Includes all reservation statuses
- Paginates results
- Sorts by most recent first
- Calculates late return flag for each record
- Includes book information
- Returns pagination metadata

---

## API Endpoints to Implement

Based on `api-contracts.md`, implement these endpoints:

### POST /api/reservations
- **Access:** Requires authentication (Patron or Librarian)
- **Request:** bookId
- **Success (201):** reservationId, bookId, userId, bookTitle, status, reservedAt, expiresAt, message
- **Error (400):** RESERVATION_LIMIT_EXCEEDED or BOOK_UNAVAILABLE

### GET /api/reservations
- **Access:** Requires authentication (Patron or Librarian)
- **Success (200):** Array of active reservations with daysUntilExpiry/daysUntilDue, totalActive count

### POST /api/reservations/{reservationId}/checkout
- **Access:** Requires Librarian role
- **Request:** notes (optional)
- **Success (200):** reservationId, status, checkedOutAt, dueDate, message
- **Error (403):** FORBIDDEN (non-librarian)
- **Error (400):** INVALID_STATUS (not Reserved)

### POST /api/reservations/{reservationId}/return
- **Access:** Requires Librarian role
- **Request:** condition (Good, Fair, Poor, Damaged), notes (optional)
- **Success (200):** reservationId, returnedAt, lateDays, lateFee, message
- **Error (403):** FORBIDDEN (non-librarian)
- **Error (400):** INVALID_STATUS (not CheckedOut)

### GET /api/reservations/history
- **Access:** Requires authentication (Patron or Librarian)
- **Query Parameters:** page (default: 0), size (default: 20)
- **Success (200):** Paginated history with wasLate flag, includes pagination metadata

---

## Acceptance Criteria

- [ ] Patron can reserve books when fewer than 5 active reservations
- [ ] Reservation creation returns 400 when limit of 5 reached
- [ ] Reservation creation returns 400 when book has no available copies
- [ ] Expiration date set to 7 days from reservation date
- [ ] Active reservations show days until expiry (Reserved status)
- [ ] Active reservations show days until due (CheckedOut status)
- [ ] Checkout sets due date to 14 days from checkout date
- [ ] Only Librarian can access checkout endpoint (403 for Patron)
- [ ] Only Librarian can access return endpoint (403 for Patron)
- [ ] Late fees calculated at $1.00 per day
- [ ] Available copies decrements on reservation creation
- [ ] Available copies increments on book return
- [ ] Borrowing history shows all reservations with pagination
- [ ] wasLate flag correctly calculated in history
- [ ] Cannot checkout reservation that is not Reserved (400 error)
- [ ] Cannot return reservation that is not CheckedOut (400 error)

---

## Suggested Approach

1. Implement reservation creation with validation logic
2. Add available copies update mechanism
3. Implement active reservations retrieval with calculations
4. Create checkout endpoint with role validation
5. Create return endpoint with late fee calculation
6. Implement borrowing history with pagination
7. Add date/time calculation utilities
8. Ensure atomic operations for data consistency
9. Test all business rules and edge cases

**Note:** You have flexibility in how you structure your services, implement date calculations, handle transactions, and organize your business logic. Focus on meeting the business requirements and maintaining data consistency.

---

## Resources

- Refer to `user-stories.md` for US-007, US-008, US-009, US-010, US-011 details
- Refer to `api-contracts.md` for exact request/response formats
- DateTime and TimeSpan documentation for date/time calculations