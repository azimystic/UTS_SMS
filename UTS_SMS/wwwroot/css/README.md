# CSS Theme Files

## Main Theme File

**📄 `theme.css`** - This is your main theme file for the entire School Management System application.

### What's Inside

This consolidated file contains all the custom styling for the application, organized into 11 main sections:

1. **CSS Variables & Design Tokens** - Color palette, spacing, typography
2. **Layout Styles** - Sidebar, navbar, main layout structure
3. **Component Styles** - Buttons, badges, cards, modals, alerts
4. **Form Styles** - Inputs, selects, checkboxes, validation states
5. **Table Styles** - Tables, pagination, filters, sorting
6. **Dashboard Styles** - Dashboard cards, charts, statistics
7. **Detail Page Styles** - Hero sections, timelines, info cards
8. **Animation Styles** - Keyframes, transitions, loading states
9. **Responsive Styles** - Mobile, tablet, desktop breakpoints
10. **Utility Classes** - Helper classes for quick styling
11. **Design System (CREXTIO Theme)** - Alternative warm modern aesthetic

### How to Edit the Theme

To change the theme of the entire application, simply edit `theme.css`:

#### Quick Customization Guide

- **Change primary colors**: Edit variables starting at line 28 (e.g., `--color-primary`)
- **Change fonts**: Edit `--font-family` at line 147
- **Modify button styles**: Go to line 753
- **Adjust card styling**: Go to line 1621
- **Change spacing**: Edit `--spacing` variables at line 124
- **Modify mobile/tablet breakpoints**: Go to responsive section at line 4246

#### Example: Changing Primary Color

```css
/* Find this in the :root section (around line 28) */
:root {
    --color-primary: #3B82F6;  /* Change this hex code to your desired color */
    --color-primary-50: #EFF6FF;
    --color-primary-100: #DBEAFE;
    /* ... etc */
}
```

### Other Files

- **`site.css`** - Contains Tailwind CSS utility classes (auto-generated, don't edit)
- **`fontawesome.css`** - Font Awesome icon styles (third-party library)
- **Bootstrap CSS** - Located in `/lib/bootstrap/dist/css/` (third-party library)

### Benefits of Single File

✅ **Easy to edit** - One file to change the entire app theme  
✅ **Better performance** - Single HTTP request instead of 11  
✅ **Well documented** - Clear table of contents and section markers  
✅ **Version control friendly** - Easier to track changes  
✅ **No conflicts** - All styles in one place, no cascading issues  

### Need Help?

The `theme.css` file has extensive comments and a detailed table of contents at the top. Each section is clearly marked with headers like:

```css
/* ========================================
   SECTION X: SECTION NAME
   Description of what this section contains
   ======================================== */
```

---

**Last Updated**: January 15, 2026  
**Size**: ~157KB (6,944 lines)  
**Version**: 1.0
