# Frontend Coding Standards & UI/UX Guidelines

## Overview
This document defines the coding standards and UI/UX guidelines for the e-commerce B2B admin panel and web application. These rules are **MANDATORY** and must be followed in all frontend development work.

---

## Table of Contents
1. [Coding Standards](#coding-standards)
2. [UI/UX Guidelines](#uiux-guidelines)
3. [Mobile-First Design](#mobile-first-design)
4. [Component Architecture](#component-architecture)
5. [Error Handling](#error-handling)

---

## Coding Standards

### STRICT Rules (MANDATORY)

#### 1. No Console Logs
**Rule:** Never leave `console.log`, `console.debug`, `console.warn`, or `console.error` statements in production code.

**Why:** Console logs can expose sensitive information and impact performance.

**Example:**
```csharp
// ❌ BAD
Console.WriteLine($"[Service] Processing order: {orderId}");

// ✅ GOOD
// Use proper logging framework (ILogger, Serilog, etc.)
_logger.LogInformation("Processing order: {OrderId}", orderId);
```

#### 2. No `any` Type (TypeScript Strict Typing)
**Rule:** Always use explicit types. Never use `any` type.

**Why:** Type safety prevents runtime errors and improves code maintainability.

**Example:**
```typescript
// ❌ BAD
function processData(data: any) {
    return data.value;
}

// ✅ GOOD
interface DataModel {
    value: number;
    name: string;
}

function processData(data: DataModel): number {
    return data.value;
}
```

#### 3. No Magic Numbers
**Rule:** Extract all numeric literals to named constants with descriptive names.

**Why:** Magic numbers make code hard to understand and maintain.

**Example:**
```csharp
// ❌ BAD
if (items.Count > 10) { ... }
await Task.Delay(5000);

// ✅ GOOD
private const int MAX_ITEMS_PER_PAGE = 10;
private const int API_TIMEOUT_MS = 5000;

if (items.Count > MAX_ITEMS_PER_PAGE) { ... }
await Task.Delay(API_TIMEOUT_MS);
```

#### 4. No Single-Letter Variable Names
**Rule:** Use descriptive, self-documenting variable names.

**Why:** Code readability and maintainability.

**Example:**
```csharp
// ❌ BAD
var x = GetOrders();
var p = x.First();

// ✅ GOOD
var orders = GetOrders();
var firstOrder = orders.First();
```

#### 5. Function Length Limit
**Rule:** Functions must not exceed 50 lines of code.

**Why:** Long functions are hard to understand, test, and maintain.

**Example:**
```csharp
// ❌ BAD - 80 lines
public async Task ProcessOrder(Order order) {
    // 80 lines of code...
}

// ✅ GOOD - Split into smaller functions
public async Task ProcessOrder(Order order) {
    await ValidateOrder(order);
    await CalculateTotals(order);
    await ApplyDiscounts(order);
    await SaveOrder(order);
}

private async Task ValidateOrder(Order order) { /* ... */ }
private async Task CalculateTotals(Order order) { /* ... */ }
private async Task ApplyDiscounts(Order order) { /* ... */ }
private async Task SaveOrder(Order order) { /* ... */ }
```

#### 6. No Nested Callback Hell
**Rule:** Avoid deeply nested callbacks. Use async/await, promises, or proper error handling patterns.

**Why:** Nested callbacks make code unreadable and hard to maintain.

**Example:**
```csharp
// ❌ BAD - Callback hell
GetData(data => {
    ProcessData(data, result => {
        SaveResult(result, saved => {
            NotifyUser(saved, notified => {
                // Too nested!
            });
        });
    });
});

// ✅ GOOD - Async/await
public async Task ProcessDataFlow() {
    var data = await GetDataAsync();
    var result = await ProcessDataAsync(data);
    var saved = await SaveResultAsync(result);
    await NotifyUserAsync(saved);
}
```

---

## UI/UX Guidelines

### Mobile-First Design Principles

#### 1. Single-Column Layout on Mobile
**Rule:** All layouts must stack vertically on mobile devices (< 768px).

**Implementation:**
```css
/* Mobile-first: Stack by default */
.campaign-grid {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

/* Desktop: Multi-column */
@media (min-width: 768px) {
    .campaign-grid {
        flex-direction: row;
        flex-wrap: wrap;
    }
}
```

#### 2. Compact Campaign Cards
**Rule:** Campaign cards must be compact, information-dense, and scannable.

**Structure:**
- Discount badge (top-right, small)
- Thin accent line (top border, 2-3px)
- Title (max 2 lines, truncated with ellipsis)
- Description (max 1 line, truncated)
- Date info (secondary, muted text)
- Single full-width CTA button

**Example:**
```razor
<div class="campaign-card">
    <div class="campaign-badge">%@discount.Percentage</div>
    <div class="campaign-accent"></div>
    <h3 class="campaign-title">@discount.Name</h3>
    <p class="campaign-description">@discount.Description</p>
    <div class="campaign-dates">
        <span>@discount.StartDate - @discount.EndDate</span>
    </div>
    <button class="campaign-cta">Ürünleri Gör</button>
</div>
```

#### 3. Touch-Friendly Spacing
**Rule:** All interactive elements must have a minimum touch target of 44px × 44px.

**Implementation:**
```css
.campaign-cta,
.btn,
a.button {
    min-height: 44px;
    min-width: 44px;
    padding: 12px 24px; /* Ensures 44px minimum */
}
```

#### 4. Typography Hierarchy
**Rule:** Use clear typographic hierarchy with appropriate font weights and sizes.

**Structure:**
- **Title:** Bold, 16-18px, max 2 lines
- **Description:** Regular, 14px, max 1 line
- **Metadata:** Muted, 12px, secondary color

---

## Component Architecture

### Reusable Components

#### 1. Component Splitting
**Rule:** Break down large components into smaller, focused, reusable components.

**Example:**
```razor
<!-- ❌ BAD - Monolithic component -->
<CampaignSection>
    <!-- 200 lines of mixed logic -->
</CampaignSection>

<!-- ✅ GOOD - Composed components -->
<CampaignSection>
    <CampaignCard Discount="@discount" />
    <CampaignCard Discount="@discount" />
</CampaignSection>

<!-- CampaignCard.razor - Focused, reusable -->
<div class="campaign-card">
    <CampaignBadge Percentage="@Discount.Percentage" />
    <CampaignContent Title="@Discount.Name" Description="@Discount.Description" />
    <CampaignDates StartDate="@Discount.StartDate" EndDate="@Discount.EndDate" />
    <CampaignCTA OnClick="@HandleViewProducts" />
</div>
```

#### 2. Component Props/Parameters
**Rule:** Use strongly-typed parameters with clear naming.

**Example:**
```csharp
// ✅ GOOD
[Parameter] public DiscountDto Discount { get; set; } = null!;
[Parameter] public EventCallback<int> OnViewProducts { get; set; }
[Parameter] public string? CssClass { get; set; }
```

#### 3. Component State Management
**Rule:** Keep component state local and minimal. Use services for shared state.

**Example:**
```csharp
// ✅ GOOD - Local state
private bool isExpanded = false;
private string? selectedFilter = null;

// ✅ GOOD - Shared state via service
[Inject] protected ICartStateService CartStateService { get; set; } = null!;
```

---

## Error Handling

### Async Logic Error Handling

**Rule:** All async operations MUST have proper error handling (try/catch or equivalent).

**Example:**
```csharp
// ❌ BAD - No error handling
protected async Task LoadData() {
    var data = await Service.GetDataAsync();
    ProcessData(data);
}

// ✅ GOOD - Proper error handling
protected async Task LoadData() {
    try {
        isLoading = true;
        var data = await Service.GetDataAsync();
        ProcessData(data);
    }
    catch (Exception ex) {
        _logger.LogError(ex, "Failed to load data");
        await NotificationService.Notify(
            new NotificationMessage {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = "Veriler yüklenirken bir hata oluştu.",
                Duration = 4000
            }
        );
    }
    finally {
        isLoading = false;
    }
}
```

### Error Handling Patterns

#### 1. User-Facing Errors
**Rule:** Always show user-friendly error messages. Never expose technical details to end users.

```csharp
// ✅ GOOD
catch (Exception ex) {
    _logger.LogError(ex, "Technical details here");
    await ShowUserFriendlyError("İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.");
}
```

#### 2. Validation Errors
**Rule:** Validate input before processing and show clear validation messages.

```csharp
// ✅ GOOD
if (string.IsNullOrWhiteSpace(searchText)) {
    await NotificationService.Notify(
        new NotificationMessage {
            Severity = NotificationSeverity.Warning,
            Summary = "Uyarı",
            Detail = "Lütfen arama metni giriniz.",
            Duration = 3000
        }
    );
    return;
}
```

---

## CSS & Styling Guidelines

### Mobile-First Responsive CSS

**Rule:** Write mobile styles first, then use media queries for larger screens.

**Example:**
```css
/* Mobile-first: Base styles */
.campaign-card {
    width: 100%;
    padding: 1rem;
    margin-bottom: 1rem;
}

/* Tablet: 768px and up */
@media (min-width: 768px) {
    .campaign-card {
        width: calc(50% - 0.5rem);
        display: inline-block;
    }
}

/* Desktop: 1024px and up */
@media (min-width: 1024px) {
    .campaign-card {
        width: calc(33.333% - 0.667rem);
    }
}
```

### CSS Naming Conventions

**Rule:** Use BEM (Block Element Modifier) or similar consistent naming.

**Example:**
```css
/* ✅ GOOD - BEM naming */
.campaign-card { }
.campaign-card__badge { }
.campaign-card__badge--discount { }
.campaign-card__title { }
.campaign-card__description { }
```

### CSS Variables

**Rule:** Use CSS custom properties (variables) for colors, spacing, and other design tokens.

**Example:**
```css
:root {
    --b2b-primary: #0e947a;
    --b2b-text-main: #0f172a;
    --b2b-spacing-sm: 0.5rem;
    --b2b-spacing-md: 1rem;
    --b2b-spacing-lg: 1.5rem;
}

.campaign-card {
    padding: var(--b2b-spacing-md);
    color: var(--b2b-text-main);
}
```

---

## Performance Guidelines

### 1. Lazy Loading
**Rule:** Lazy load images and heavy components.

**Example:**
```razor
<!-- ✅ GOOD -->
<img src="@imageUrl" loading="lazy" alt="@productName" />
```

### 2. Debouncing
**Rule:** Debounce user input for search and filter operations.

**Example:**
```csharp
// ✅ GOOD - Debounced search
private System.Threading.Timer? searchDebounceTimer;

protected void OnSearchInput(string value) {
    searchDebounceTimer?.Dispose();
    searchDebounceTimer = new System.Threading.Timer(
        async _ => await PerformSearch(value),
        null,
        TimeSpan.FromMilliseconds(300), // 300ms debounce
        Timeout.InfiniteTimeSpan
    );
}
```

### 3. Virtualization
**Rule:** Use virtualization for long lists (100+ items).

**Example:**
```razor
<!-- ✅ GOOD - RadzenDataGrid with paging -->
<RadzenDataGrid Data="@items" 
                AllowPaging="true" 
                PageSize="20" />
```

---

## Accessibility Guidelines

### 1. Semantic HTML
**Rule:** Use semantic HTML elements (nav, main, article, section, etc.).

### 2. ARIA Labels
**Rule:** Add ARIA labels for screen readers when needed.

**Example:**
```razor
<!-- ✅ GOOD -->
<button aria-label="Sepete ekle: @productName">
    <i class="fa-solid fa-cart-plus"></i>
</button>
```

### 3. Keyboard Navigation
**Rule:** Ensure all interactive elements are keyboard accessible.

### 4. Color Contrast
**Rule:** Maintain WCAG AA contrast ratios (4.5:1 for normal text, 3:1 for large text).

---

## Testing Guidelines

### 1. Component Testing
**Rule:** Write unit tests for complex business logic.

### 2. Integration Testing
**Rule:** Test component interactions and data flow.

### 3. Visual Regression
**Rule:** Use visual regression testing for UI components.

---

## Code Review Checklist

Before submitting code for review, ensure:

- [ ] No console.log statements
- [ ] No `any` types (TypeScript)
- [ ] No magic numbers (all extracted to constants)
- [ ] No single-letter variable names
- [ ] All functions are under 50 lines
- [ ] No nested callback hell
- [ ] All async operations have error handling
- [ ] Mobile-first responsive design
- [ ] Touch-friendly spacing (44px minimum)
- [ ] Proper component splitting
- [ ] Semantic HTML and accessibility
- [ ] CSS follows naming conventions
- [ ] Performance optimizations applied

---

## Examples

### Campaign Card Component (Reference Implementation)

```razor
@* CampaignCard.razor *@
@using ecommerce.Domain.Shared.Dtos.Discount

<div class="campaign-card">
    @if (Discount.UsePercentage && Discount.DiscountPercentage.HasValue)
    {
        <div class="campaign-badge campaign-badge--discount">
            %@Discount.DiscountPercentage.Value.ToString("N0")
        </div>
    }
    <div class="campaign-accent"></div>
    
    <h3 class="campaign-title" title="@Discount.Name">
        @Discount.Name
    </h3>
    
    @if (!string.IsNullOrEmpty(Discount.Description))
    {
        <p class="campaign-description" title="@Discount.Description">
            @Discount.Description
        </p>
    }
    
    <div class="campaign-dates">
        @if (Discount.StartDate.HasValue)
        {
            <span class="campaign-date-item">
                <i class="fa-solid fa-calendar-check"></i>
                Başlangıç: @Discount.StartDate.Value.ToLocalTime().ToString("dd.MM.yyyy")
            </span>
        }
        @if (Discount.EndDate.HasValue)
        {
            <span class="campaign-date-item">
                <i class="fa-solid fa-calendar-times"></i>
                Bitiş: @Discount.EndDate.Value.ToLocalTime().ToString("dd.MM.yyyy")
            </span>
        }
    </div>
    
    <button class="campaign-cta" 
            @onclick="HandleViewProducts"
            aria-label="Kampanya ürünlerini görüntüle: @Discount.Name">
        <i class="fa-solid fa-search"></i>
        <span>Ürünleri Gör</span>
    </button>
</div>

@code {
    [Parameter, EditorRequired] 
    public DiscountDto Discount { get; set; } = null!;
    
    [Parameter] 
    public EventCallback<DiscountDto> OnViewProducts { get; set; }
    
    private async Task HandleViewProducts()
    {
        try
        {
            await OnViewProducts.InvokeAsync(Discount);
        }
        catch (Exception ex)
        {
            // Error handling via parent component or service
        }
    }
}
```

```css
/* CampaignCard.css - Mobile-first */
.campaign-card {
    position: relative;
    background: var(--b2b-surface);
    border: 1px solid var(--b2b-border);
    border-radius: var(--b2b-radius);
    padding: var(--b2b-spacing-md);
    margin-bottom: var(--b2b-spacing-md);
    display: flex;
    flex-direction: column;
    gap: var(--b2b-spacing-sm);
}

.campaign-badge {
    position: absolute;
    top: var(--b2b-spacing-sm);
    right: var(--b2b-spacing-sm);
    background: var(--b2b-primary);
    color: #ffffff;
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 0.75rem;
    font-weight: 700;
    z-index: 1;
}

.campaign-accent {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 3px;
    background: var(--b2b-primary);
    border-radius: var(--b2b-radius) var(--b2b-radius) 0 0;
}

.campaign-title {
    font-size: 1rem;
    font-weight: 600;
    color: var(--b2b-text-main);
    margin: var(--b2b-spacing-sm) 0 0 0;
    line-height: 1.4;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
    text-overflow: ellipsis;
}

.campaign-description {
    font-size: 0.875rem;
    color: var(--b2b-text-secondary);
    margin: 0;
    line-height: 1.4;
    display: -webkit-box;
    -webkit-line-clamp: 1;
    -webkit-box-orient: vertical;
    overflow: hidden;
    text-overflow: ellipsis;
}

.campaign-dates {
    display: flex;
    flex-direction: column;
    gap: 4px;
    margin-top: auto;
    font-size: 0.75rem;
    color: var(--b2b-text-muted);
}

.campaign-date-item {
    display: flex;
    align-items: center;
    gap: 4px;
}

.campaign-cta {
    width: 100%;
    min-height: 44px;
    padding: 12px 24px;
    background: var(--b2b-primary);
    color: #ffffff;
    border: none;
    border-radius: var(--b2b-radius);
    font-weight: 600;
    font-size: 0.875rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    margin-top: var(--b2b-spacing-sm);
    transition: background-color 0.2s ease;
}

.campaign-cta:hover {
    background: var(--b2b-primary-dark);
}

.campaign-cta:active {
    transform: scale(0.98);
}

/* Tablet and up */
@media (min-width: 768px) {
    .campaign-card {
        width: calc(50% - var(--b2b-spacing-md) / 2);
    }
}

/* Desktop */
@media (min-width: 1024px) {
    .campaign-card {
        width: calc(33.333% - var(--b2b-spacing-md) * 2 / 3);
    }
}
```

---

## Summary

These standards ensure:
- **Consistency:** All developers follow the same patterns
- **Maintainability:** Code is easy to read and modify
- **Performance:** Optimized for mobile and desktop
- **Accessibility:** Usable by all users
- **Quality:** Production-ready, error-handled code

**Remember:** These rules are **MANDATORY**. Always refer to this document when writing frontend code.
