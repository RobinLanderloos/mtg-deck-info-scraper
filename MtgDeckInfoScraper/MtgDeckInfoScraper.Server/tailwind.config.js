/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./**/*.{razor,html}",
        "./**/(Layout|Pages)/*.{razor,html}"
    ],
    theme: {
        extend: {},
        container: {
            center: true
        }
    },
    plugins: [
        require('daisyui')
    ],
}

