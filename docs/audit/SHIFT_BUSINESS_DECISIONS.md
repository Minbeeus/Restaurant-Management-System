# SHIFT BUSINESS DECISIONS RECORD
**Project:** Restaurant Management System (POS / KDS)  
**Date:** 2026-11-02  
**Framework:** Shared Cash Drawer – Cash Custodian – Strict Shift Reconciliation  

---

## 1. Context & Business Problem
A modern restaurant operation requires strict cash accountability while maintaining smooth handover and multi-cashier POS operations. The previous design treated shifts as individual cashier drawers, leading to confusion when multiple staff members handled orders and cash in the same operational period.

To establish enterprise-grade financial governance, 8 core architectural decisions were formalized and implemented.

---

## 2. Decision Log

### Decision 1: Single Shared Cash Drawer per Register
- **Decision:** Each active register operates a single shared cash drawer during a shift.
- **Rationale:** Physical POS terminals have one cash drawer. All cashiers working at the terminal put cash into and make change from this single drawer.

### Decision 2: Manager-Initiated Shift Opening with Cash Custodian PIN
- **Decision:** Only a Manager or Admin can open a shift. When opening, the Manager inputs the opening float (`InitialCash`) and specifies the primary cashier responsible for the drawer by validating their 6-digit PIN (`CashCustodianPin`).
- **Rationale:** Establishes formal supervisory custody transfer from vault to POS drawer.

### Decision 3: Cashier Role Lockdown (No Open, Close, or Till-Out)
- **Decision:** Cashiers are strictly restricted to processing sales, viewing their shift sales, and viewing the drawer movements. They cannot open or close shifts or withdraw cash (till-out).
- **Rationale:** Enforces segregation of duties (SoD) between frontline order processing and cash custody management.

### Decision 4: Per-Payment Cashier Attribution
- **Decision:** Every cash transaction records the exact cashier who collected the payment via `Payment.ProcessedByUserId`.
- **Rationale:** Enables granular sales reporting per cashier while maintaining a unified drawer balance.

### Decision 5: Mandatory Drawer Cash Withdrawal (Till-Out) Prior to Shift Closure
- **Decision:** At shift end, 100% of the physical cash in the drawer must be tilled out to the manager/safe ($\text{RemainingCashToTillOut} = 0$).
- **Rationale:** Ensures no overnight cash float is left unattended in the POS hardware drawer; all cash is reconciled and deposited into the central safe.

### Decision 6: Zero Cash Discrepancy Requirement
- **Decision:** The shift cannot be closed if $\text{CashDifference} \neq 0$. Any discrepancy must be investigated, reconciled, or documented via authorized cash adjustments before closure.
- **Rationale:** Prevents unaccounted cash losses from being swept under the rug.

### Decision 7: Complete Order Settlement Prerequisite
- **Decision:** A shift cannot be closed if there are any uncompleted (`Status != Completed`) or unpaid (`PaymentStatus != Paid`) orders.
- **Rationale:** Prevents orphaned open tables, unpaid guest checks, and kitchen backlogs from rolling into subsequent shifts without accountability.

### Decision 8: Standardized 6-Digit Numeric Employee PIN
- **Decision:** All POS login and custodian PINs are derived as `EmployeeCode.PadLeft(6, '0')` and stored hashed with BCrypt.
- **Rationale:** Guarantees a consistent, tamper-proof, memory-friendly authentication experience for fast keypad entry on POS touchscreens.
