##### Financial no mobile

The main issue is almost certainly a missing **viewport meta tag** in your HTML. Without it, mobile Chrome assumes a desktop-sized viewport (around 980px wide) and scales your page down, making everything appear tiny and preventing proper rotation handling. [developer.mozilla](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/meta/name/viewport)

## Essential requirements for mobile-friendly React apps

### 1. Viewport meta tag (critical)

Add this to your `index.html` file in the `<head>` section:

```html
<meta name="viewport" content="width=device-width, initial-scale=1" />
```

This tells mobile browsers to:
- Render at the device's actual width (`width=device-width`)
- Start at 100% zoom (`initial-scale=1`)
- Enable responsive CSS media queries to work correctly [developer.mozilla](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/meta/name/viewport)

Without this tag, your React app will render as if on a desktop and just zoomed out, which is why rotation doesn't help—the layout itself isn't responsive. [rankosaur](https://rankosaur.com/en/wiki/mobile-first-indexing)

### 2. Responsive CSS

Ensure your React components use responsive techniques:

- **Relative units**: Use `%`, `vw`, `vh`, `rem`, or `em` instead of fixed `px` widths
- **Media queries**: Add breakpoints for different screen sizes
- **Flexible layouts**: Use Flexbox or CSS Grid that adapts to container width
- **Max-width constraints**: Prevent content from exceeding viewport width [rankosaur](https://rankosaur.com/en/wiki/mobile-first-indexing)

Example CSS pattern:
```css
.container {
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 1rem;
}

@media (max-width: 768px) {
  .container {
    padding: 0.5rem;
  }
}
```

### 3. Mobile viewport height fix

If you're using `height: 100vh` for full-screen layouts, switch to `height: 100dvh` on mobile. Mobile browsers have dynamic address bars that make `100vh` unreliable, causing content to be cut off at the bottom. [21st](https://21st.dev/blog/react-mobile-navigation-components)

```css
.fullscreen {
  height: 100dvh; /* Dynamic viewport height */
}
```

### 4. Check rotation settings

If rotation still doesn't work after adding the viewport tag:

- **Android**: Ensure Auto-rotate is enabled in Settings > Display > Auto-rotate screen [showu](https://showu.net/phone-screen-rotation-not-working-how-to-fix-it/)
- **iPhone**: Make sure Portrait Orientation Lock is off in Control Center [showu](https://showu.net/phone-screen-rotation-not-working-how-to-fix-it/)
- Test rotation in other apps to rule out device-level issues [showu](https://showu.net/phone-screen-rotation-not-working-how-to-fix-it/)

## Quick checklist

- [ ] Add `<meta name="viewport" content="width=device-width, initial-scale=1" />` to `public/index.html`
- [ ] Replace fixed pixel widths with responsive units
- [ ] Test with Chrome DevTools mobile emulation (F12 → toggle device toolbar)
- [ ] Verify auto-rotate is enabled on your phone
- [ ] Use `100dvh` instead of `100vh` for full-height elements [21st](https://21st.dev/blog/react-mobile-navigation-components)

The viewport meta tag alone will likely solve 90% of your issue. [rankosaur](https://rankosaur.com/en/wiki/mobile-first-indexing)