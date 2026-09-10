---
project: SuperFood
document: Product Specification & User Story Backlog
version: 1.0
last_updated: 2026-09-10
status: draft — approved for planning, not yet implemented
tech_stack: not decided (intentionally out of scope for this document)
---

# Purpose & How to Use This Document

This document is the source of truth for **what** to build, independent of **how**
(no framework, language, or infrastructure choices are made here).

Conventions for any human or AI agent implementing from this file:

- Every user story has a **stable ID** (`US-EEss`, where `EE` = epic number, `ss` =
  story number within the epic, e.g. `US-0103`). Reference this ID in commit
  messages, PR titles, and test names (e.g. `test_US-0103_create_product`).
- Each story has explicit **Acceptance Criteria** as a checklist. A story is only
  "done" when every box is satisfied by working, tested software.
- `Priority` uses MoSCoW: `Must`, `Should`, `Could`.
- `Depends on` lists other story IDs that must exist first (referenced by ID, not
  by re-explaining them).
- Do not invent scope beyond what's listed. If a gap is found during
  implementation, add a new story with a new ID rather than silently expanding
  an existing one — keep the backlog as the single source of truth.
- Section 4 ("Out of Scope") is as important as the stories themselves — it
  tells an implementer what NOT to build yet.

---

## 1. Overview

SuperFood is a **multi-tenant restaurant management SaaS**.

- A **Platform Admin** (SaaS operator) manages restaurant tenants at a platform
  level.
- Each **Restaurant** (tenant) independently manages its own menu, tables,
  staff, roles, and orders.
- **Customers** interact directly with a restaurant's menu to place dine-in
  (table/QR-based) or delivery orders — this is a customer-facing product, not
  only an internal back-office tool.
- Delivery is **order capture only** in this iteration: no courier/rider
  assignment or live delivery tracking.
- Billing/subscription management for tenants is **out of scope** for this
  iteration.

---

## 2. Roles

| Role | Scope | Description |
|---|---|---|
| `platform_admin` | Platform | Employee of the SaaS provider. Manages restaurant tenant accounts. |
| `restaurant_owner` | Restaurant | Full control of one restaurant: settings, menu, tables, staff, roles, orders. |
| `restaurant_manager` | Restaurant | Same operational permissions as owner, typically without account-level controls (e.g. cannot delete the restaurant). |
| `waiter` | Restaurant | Manages tables and on-site orders. |
| `kitchen` | Restaurant | Updates order/item preparation status. |
| `cashier` | Restaurant | Registers payments against orders. (May be merged with `waiter` in small restaurants.) |
| `customer` | Public | End diner. No login required to browse or order. |

Roles are assignable per staff member and a staff member may hold multiple
roles (see Epic 3).

---

## 3. Domain Entities (conceptual — not a schema)

| Entity | Key Attributes | Notes |
|---|---|---|
| `Restaurant` | name, logo, address, hours, contact, status (active/suspended) | Tenant root. |
| `StaffUser` | name, email, roles[], active | Belongs to one `Restaurant`. |
| `Role` | name, permissions[] | Custom per restaurant, from Epic 3. |
| `Category` | name, sortOrder, visible | Belongs to a `Restaurant`. |
| `Product` | name, description, price, photo, category, variations[], available, active | Belongs to a `Category`. |
| `ProductVariation` | name (e.g. size), priceAdjustment | Belongs to a `Product`. |
| `Table` | identifier, capacity, qrCode, status (free/occupied/awaiting_payment) | Belongs to a `Restaurant`. |
| `TableSession` | table, openedAt, closedAt | Groups orders for one seating. |
| `Order` | type (dine_in/delivery), items[], status, paymentStatus, paymentMethod | Linked to a `TableSession` (dine-in) or delivery address (delivery). |
| `OrderItem` | product, chosenVariation, quantity, notes, status (pending/preparing/ready) | Belongs to an `Order`. |
| `DeliveryDetails` | customerName, address, contact | Attached to a delivery `Order`. |

---

## 4. Out of Scope (this iteration)

Explicitly deferred — do not build unless a new story is added:

- Billing/subscription management, plans, invoicing for restaurant tenants.
- Delivery courier/rider assignment, live GPS tracking, dispatch logic.
- Payment gateway integration details (only payment *status/method recording*
  is in scope, not processing).
- Multi-language / multi-currency support.
- Analytics/reporting dashboards beyond basic order history (see US-0904).
- Customer accounts/authentication beyond optionally saving delivery details
  (US-1005, marked `Could`).
- Inventory/stock management beyond a simple available/unavailable toggle.

---

## 5. Epics & User Stories

### EPIC-01 — Platform Administration
Goal: let the Platform Admin manage restaurant tenants operationally.

- **US-0101** — As a `platform_admin`, I want to create a new restaurant
  account, so that a new tenant can start using the platform.
  - Acceptance Criteria:
    - [ ] Capture restaurant name and contact info.
    - [ ] Create an initial `restaurant_owner` user for the tenant.
    - [ ] Owner receives credentials/invite to access their account.
  - Priority: Must

- **US-0102** — As a `platform_admin`, I want to view a list of all
  restaurants with their status, so that I can monitor the platform.
  - Acceptance Criteria:
    - [ ] List shows name, status (active/suspended), creation date.
    - [ ] List is searchable/filterable by status.
  - Priority: Must
  - Depends on: US-0101

- **US-0103** — As a `platform_admin`, I want to suspend or reactivate a
  restaurant account, so that I can enforce platform policy without deleting
  data.
  - Acceptance Criteria:
    - [ ] Suspending blocks all staff and customer access to that restaurant.
    - [ ] Reactivating restores access without data loss.
  - Priority: Must
  - Depends on: US-0101

- **US-0104** — As a `platform_admin`, I want to view a restaurant's basic
  details (owner, staff count, creation date), so that I can support them.
  - Priority: Should
  - Depends on: US-0101

- **US-0105** — As a `platform_admin`, I want to permanently deactivate/delete
  a restaurant, so that I can offboard tenants that leave the platform.
  - Acceptance Criteria:
    - [ ] Action requires confirmation (destructive).
    - [ ] Deactivated restaurant's public menu/ordering pages stop serving.
  - Priority: Should
  - Depends on: US-0101

---

### EPIC-02 — Restaurant Settings & Onboarding
Goal: let a restaurant configure its public identity and availability.

- **US-0201** — As a `restaurant_owner`, I want to complete my restaurant's
  profile (name, logo, address, hours, contact), so that customers see
  accurate information.
  - Priority: Must

- **US-0202** — As a `restaurant_owner`, I want to set operating hours per
  day, so that customers know when ordering is available.
  - Acceptance Criteria:
    - [ ] Ordering is blocked for customers outside operating hours, with a
          clear message.
  - Priority: Must
  - Depends on: US-0201

- **US-0203** — As a `restaurant_owner`, I want to enable/disable order types
  independently (dine-in, delivery), so that I can control which channels are
  active.
  - Priority: Must
  - Depends on: US-0201

---

### EPIC-03 — User & Role Management
Goal: let a restaurant control staff access.

- **US-0301** — As a `restaurant_owner`, I want to invite staff members by
  email, so that they can access the system.
  - Priority: Must

- **US-0302** — As a `restaurant_owner`, I want to define roles (e.g.
  Manager, Waiter, Kitchen, Cashier) with specific permissions, so that staff
  only access what's relevant to them.
  - Acceptance Criteria:
    - [ ] Permissions are granular per feature area (menu, tables, orders,
          users).
    - [ ] Restaurant starts with the default roles listed in Section 2.
  - Priority: Must

- **US-0303** — As a `restaurant_owner`, I want to assign one or more roles
  to a staff member, so that access matches their job.
  - Priority: Must
  - Depends on: US-0301, US-0302

- **US-0304** — As a `restaurant_owner`, I want to deactivate a staff
  member's access, so that former employees can't log in.
  - Priority: Must
  - Depends on: US-0301

- **US-0305** — As a `restaurant_owner`, I want to edit a role's permissions,
  so that access control can evolve with my operation.
  - Priority: Should
  - Depends on: US-0302

---

### EPIC-04 — Menu Management: Categories
Goal: let a restaurant organize its menu.

- **US-0401** — As a `restaurant_manager`, I want to create, edit, and delete
  menu categories, so that the menu is organized.
  - Priority: Must

- **US-0402** — As a `restaurant_manager`, I want to reorder categories, so
  that the menu displays in the order I prefer.
  - Priority: Should
  - Depends on: US-0401

- **US-0403** — As a `restaurant_manager`, I want to temporarily hide a
  category, so that seasonal sections can be toggled without deleting data.
  - Priority: Should
  - Depends on: US-0401

---

### EPIC-05 — Menu Management: Products
Goal: let a restaurant define what it sells.

- **US-0501** — As a `restaurant_manager`, I want to create a product with
  name, description, price, photo, and category, so that customers can see
  what's offered.
  - Priority: Must
  - Depends on: US-0401

- **US-0502** — As a `restaurant_manager`, I want to add variations/options
  to a product (e.g. size, extra toppings) with price adjustments, so that
  customers can customize orders.
  - Priority: Must
  - Depends on: US-0501

- **US-0503** — As a `restaurant_manager`, I want to mark a product as
  unavailable/out of stock, so that customers can't order it while
  unavailable.
  - Acceptance Criteria:
    - [ ] Unavailable products are visibly disabled on the customer menu, not
          hidden entirely (unless also inactive).
  - Priority: Must
  - Depends on: US-0501

- **US-0504** — As a `restaurant_manager`, I want to set a product as
  active/inactive, so that I can control what's visible without deleting it.
  - Priority: Should
  - Depends on: US-0501

- **US-0505** — As a `restaurant_manager`, I want to reorder products within
  a category, so that I can highlight best sellers.
  - Priority: Could
  - Depends on: US-0501

---

### EPIC-06 — Table Management
Goal: let a restaurant represent and manage its physical dine-in layout.

- **US-0601** — As a `restaurant_manager`, I want to create tables with an
  identifier and capacity, so that I can represent my physical layout.
  - Priority: Must

- **US-0602** — As a `restaurant_manager`, I want to generate a QR code per
  table, so that customers can scan it to view the menu and order for that
  table.
  - Priority: Must
  - Depends on: US-0601

- **US-0603** — As a `waiter`, I want to see the current status of each
  table (free, occupied, awaiting payment), so that I can manage seating.
  - Priority: Must
  - Depends on: US-0601

- **US-0604** — As a `waiter`, I want to open a table (start a session) when
  guests are seated, so that orders can be attached to it.
  - Priority: Must
  - Depends on: US-0601

- **US-0605** — As a `waiter`, I want to close a table (end session) after
  payment, so that it becomes available again.
  - Priority: Must
  - Depends on: US-0604

---

### EPIC-07 — On-Site Order Management
Goal: capture and manage dine-in orders, from both customers and staff.

- **US-0701** — As a `customer`, I want to scan a table's QR code and view
  the menu, so that I can order without waiting for a waiter.
  - Priority: Must
  - Depends on: US-0602

- **US-0702** — As a `customer`, I want to add items (with chosen variations
  and notes) to my order and submit it, so that the kitchen receives my
  request.
  - Priority: Must
  - Depends on: US-0701, US-0604

- **US-0703** — As a `waiter`, I want to create or edit an order on behalf of
  a table, so that I can take orders directly when needed.
  - Priority: Must
  - Depends on: US-0604

- **US-0704** — As a `waiter`, I want to add items to an already-open table
  order as guests order more, so that a table can have multiple rounds.
  - Priority: Must
  - Depends on: US-0604

- **US-0705** — As a `waiter`, I want to split or merge a table's bill, so
  that groups can pay separately or together.
  - Priority: Should
  - Depends on: US-0604

- **US-0706** — As a `waiter` or `cashier`, I want to mark an order as paid
  and register the payment method, so that financial records are accurate.
  - Priority: Must
  - Depends on: US-0703

---

### EPIC-08 — Delivery Order Management (capture only)
Goal: capture and progress delivery orders without courier logistics.

- **US-0801** — As a `customer`, I want to browse the menu and place a
  delivery order with my address and contact info, so that I can receive
  food at home.
  - Priority: Must
  - Depends on: US-0203

- **US-0802** — As restaurant staff, I want to see incoming delivery orders
  in a queue, so that I can start preparing them.
  - Priority: Must
  - Depends on: US-0801

- **US-0803** — As restaurant staff, I want to accept or reject a delivery
  order, so that I can manage capacity.
  - Priority: Must
  - Depends on: US-0802

- **US-0804** — As restaurant staff, I want to update a delivery order's
  status (received → preparing → ready → out for delivery →
  delivered/completed), so that progress is tracked without courier
  assignment features.
  - Priority: Must
  - Depends on: US-0803

- **US-0805** — As restaurant staff, I want to record the payment status
  (paid online / cash on delivery), so that finances are tracked.
  - Priority: Must
  - Depends on: US-0801

- **US-0806** — As a `customer`, I want to see my delivery order's current
  status, so that I know when to expect it.
  - Priority: Should
  - Depends on: US-0804

---

### EPIC-09 — Order Lifecycle & Kitchen Workflow
Goal: coordinate preparation across dine-in and delivery orders.

- **US-0901** — As `kitchen` staff, I want to see a live queue of pending
  order items across dine-in and delivery, so that I know what to prepare
  next.
  - Priority: Must
  - Depends on: US-0702, US-0801

- **US-0902** — As `kitchen` staff, I want to mark individual items or a
  whole order as "in preparation" or "ready", so that waiters/customers are
  notified.
  - Priority: Must
  - Depends on: US-0901

- **US-0903** — As a `waiter`, I want to be notified when an order is ready,
  so that I can deliver it to the table promptly.
  - Priority: Should
  - Depends on: US-0902

- **US-0904** — As a `restaurant_manager`, I want to view a history of
  completed/cancelled orders, so that I can review operations.
  - Priority: Should
  - Depends on: US-0706, US-0804

- **US-0905** — As a `restaurant_manager` or `waiter`, I want to cancel an
  order or an item with a reason, so that mistakes or unavailable items are
  handled.
  - Priority: Must
  - Depends on: US-0703, US-0802

---

### EPIC-10 — Customer Ordering Experience
Goal: give customers a clear, account-free ordering flow.

- **US-1001** — As a `customer`, I want to view the restaurant's menu with
  categories, photos, descriptions, and prices without creating an account,
  so that I can decide what to order quickly.
  - Priority: Must
  - Depends on: US-0401, US-0501

- **US-1002** — As a `customer`, I want to choose between dine-in (via table
  QR) and delivery ordering, so that I use the right flow for my situation.
  - Priority: Must
  - Depends on: US-0701, US-0801

- **US-1003** — As a `customer`, I want to review my cart before submitting,
  so that I can fix mistakes.
  - Priority: Must
  - Depends on: US-1001

- **US-1004** — As a `customer`, I want to receive confirmation that my
  order was received, so that I know it went through.
  - Priority: Must
  - Depends on: US-0702, US-0801

- **US-1005** — As a returning `customer`, I want to optionally save my
  delivery details, so that future orders are faster.
  - Priority: Could
  - Depends on: US-0801
