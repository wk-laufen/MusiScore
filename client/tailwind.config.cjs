let plugin = require('tailwindcss/plugin')

/** @type {import('tailwindcss').Config} */
export default {
  content: ["./src/**/*.{html,js,vue}"],
  theme: {
    extend: {
      colors: {
        "musi-gold": "#B18E36",
        "musi-green": "#36B151",
        "musi-blue": "#3659B1",
        "musi-red": "#B13696"
      }
    },
  },
  plugins: [
    require('tailwindcss-opentype'),
    plugin(function ({ addVariant }) {
      addVariant('has-hover', '@media(hover:hover)')
    })
  ],
}

