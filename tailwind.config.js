/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.html",
    "./**/ViewComponents.fs",
    "./**/View.fs",
    "./**/*.View.fs"
  ],
  theme: {
    extend: {
      colors: {
        gold: "#a98414b3"
      }
    },
  },
  plugins: [
    require('tailwindcss-opentype')
  ],
}
