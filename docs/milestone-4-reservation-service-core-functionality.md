### Milestone 4: Reservation Service - Core Functionality
**Goal:** Implement reservation lifecycle management

#### Deliverables:

1. **Reservation Creation**
   - Reserve available books by bookId
   - Validate user has fewer than 5 active reservations (status = Reserved or CheckedOut)
   - Verify book has availableCopies > 0
   - Set reservedAt to current timestamp
   - Set expiresAt to 7 days from reservedAt
   - Decrement book's availableCopies by 1
   - Set status to Reserved
   - Return reservation details with bookTitle
   - Error handling for limit exceeded (400) and book unavailable (400)

2. **Active Reservations View**
   - Retrieve all active reservations for authenticated user
   - Filter by status IN (Reserved, CheckedOut)
   - Calculate daysUntilExpiry for Reserved status
   - Calculate daysUntilDue for CheckedOut status
   - Include book details: bookTitle, bookAuthor
   - Return totalActive count

3. **Checkout Process**
   - Librarian-only endpoint (requires Librarian role)
   - Validate reservation status is Reserved
   - Set status to CheckedOut
   - Set checkedOutAt to current timestamp
   - Calculate dueDate (checkedOutAt + 14 days)
   - Store optional notes
   - Return formatted due date message
   - Error handling for invalid status (400) and forbidden access (403)

4. **Return Processing**
   - Librarian-only endpoint (requires Librarian role)
   - Validate reservation status is CheckedOut
   - Set status to Returned
   - Set returnedAt to current timestamp
   - Store condition (Good, Fair, Poor, Damaged)
   - Store optional notes
   - Calculate lateDays if returnedAt > dueDate
   - Calculate lateFee ($1.00 per day late)
   - Increment book's availableCopies by 1
   - Return response with late fee details if applicable
   - Error handling for invalid status (400) and forbidden access (403)

5. **Borrowing History**
   - Retrieve complete borrowing history for authenticated user
   - Include all statuses (Reserved, CheckedOut, Returned, Cancelled)
   - Paginated results: page (default: 0), pageSize (default: 20)
   - Sort by most recent first (returnedAt or reservedAt descending)
   - Calculate wasLate flag (returnedAt > dueDate)
   - Include book details: bookTitle, bookAuthor
   - Return pagination metadata: page, pageSize, totalCount, totalPages

#### Acceptance Criteria:
- [ ] Patron can reserve books when they have fewer than 5 active reservations
- [ ] Reservation limit validation returns 400 error when limit reached
- [ ] Book unavailable returns 400 error when availableCopies = 0
- [ ] expiresAt set to 7 days from reservedAt timestamp
- [ ] Active reservations show daysUntilExpiry for Reserved status
- [ ] Active reservations show daysUntilDue for CheckedOut status
- [ ] Checkout sets dueDate to 14 days from checkedOutAt
- [ ] Only Librarian role can access checkout endpoint (403 for Patron)
- [ ] Only Librarian role can access return endpoint (403 for Patron)
- [ ] Late fees calculated correctly at $1.00 per day
- [ ] availableCopies decrements on reservation creation
- [ ] availableCopies increments on book return
- [ ] Borrowing history shows all past reservations with pagination
- [ ] wasLate flag correctly calculated in history
- [ ] All reservation endpoints have 85%+ test coverage

#### API Endpoints Completed:

**1. POST /api/reservations**
- Requires authentication (Patron or Librarian)
- Request: bookId
- Response 201: reservationId, bookId, userId, bookTitle, status, reservedAt, expiresAt, message
- Error 400: RESERVATION_LIMIT_EXCEEDED or BOOK_UNAVAILABLE

**2. GET /api/reservations**
- Requires authentication (Patron or Librarian)
- Response 200: Array of active reservations with totalActive count
- Includes daysUntilExpiry (for Reserved) or daysUntilDue (for CheckedOut)

**3. POST /api/reservations/{reservationId}/checkout**
- Requires Librarian role
- Path parameter: reservationId (Guid)
- Request: notes (optional)
- Response 200: reservationId, status, checkedOutAt, dueDate, message
- Error 403: FORBIDDEN (non-librarian)
- Error 400: INVALID_STATUS (not Reserved)

**4. POST /api/reservations/{reservationId}/return**
- Requires Librarian role
- Path parameter: reservationId (Guid)
- Request: condition (Good, Fair, Poor, Damaged), notes (optional)
- Response 200: reservationId, returnedAt, lateDays, lateFee, message (includes dueDate if late)
- Error 403: FORBIDDEN (non-librarian)
- Error 400: INVALID_STATUS (not CheckedOut)

**5. GET /api/reservations/history**
- Requires authentication (Patron or Librarian)
- Query parameters: page (default: 0), pageSize (default: 20)
- Response 200: Paginated history with wasLate flag
- Includes: reservationId, bookTitle, bookAuthor, reservedAt, checkedOutAt, returnedAt, dueDate, status

#### Testing Requirements:
- Unit tests for ReservationService (business logic)
- Integration tests for complete reservation lifecycle (reserve → checkout → return)
- Concurrent reservation tests (race conditions for last available copy)
- Edge cases:
   - Reservation limit enforcement (exactly 5 active)
   - Book with 0 available copies
   - Expired reservations (past expiresAt date)
   - Overdue returns (returnedAt > dueDate)
- Late fee calculation tests (various days overdue)
- Status transition validation tests (can't checkout CheckedOut, can't return Reserved)
- Role-based access tests (Patron cannot checkout/return)
- availableCopies update tests (decrement on reserve, increment on return)
- Pagination tests for borrowing history

#### Technical Specifications:
- Use transactions for atomic operations (reserve/checkout/return)
- Date calculations using `DateTime` and `TimeSpan`
- Late fee calculation: `(returnedAt - dueDate).Days × 1.00m`
- Status enum: Reserved, CheckedOut, Returned, Cancelled
- Condition enum: Good, Fair, Poor, Damaged
- Use `Include()` for eager loading book details with reservations
- Implement proper async/await pattern for all database operations
- Use `[Authorize(Roles = "Librarian")]` for librarian-only endpoints