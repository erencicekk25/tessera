/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        paper: { DEFAULT: '#F2EDE3', deep: '#E8E0CF', edge: '#D6CBB4' },
        ink: { DEFAULT: '#1A1714', soft: '#4A443C', faint: '#8A8174' },
        vermilion: { DEFAULT: '#C8361D', soft: '#F2D3C9' },
        verdigris: { DEFAULT: '#1F6F5C', soft: '#CFE3DA' },
        ochre: { DEFAULT: '#B9850F', soft: '#F1E0B0' }
      },
      fontFamily: {
        display: ['Fraunces', 'Georgia', 'serif'],
        sans: ['"Instrument Sans"', 'system-ui', 'sans-serif'],
        mono: ['"IBM Plex Mono"', 'ui-monospace', 'monospace']
      },
      boxShadow: {
        slip: '5px 5px 0 0 #1A1714',
        'slip-sm': '3px 3px 0 0 #1A1714'
      },
      keyframes: {
        ticker: { from: { transform: 'translateX(0)' }, to: { transform: 'translateX(-50%)' } },
        'slip-in': {
          '0%': { transform: 'translateY(16px) rotate(-1.5deg)', opacity: '0' },
          '100%': { transform: 'translateY(0) rotate(0)', opacity: '1' }
        },
        'stamp-in': {
          '0%': { transform: 'scale(1.6) rotate(-14deg)', opacity: '0' },
          '60%': { transform: 'scale(0.94) rotate(-7deg)', opacity: '1' },
          '100%': { transform: 'scale(1) rotate(-7deg)', opacity: '1' }
        },
        blink: { '0%, 100%': { opacity: '1' }, '50%': { opacity: '0.25' } }
      },
      animation: {
        ticker: 'ticker 48s linear infinite',
        'slip-in': 'slip-in 420ms cubic-bezier(.2,.8,.2,1) both',
        'stamp-in': 'stamp-in 520ms cubic-bezier(.2,.8,.2,1) both',
        blink: 'blink 1.6s ease-in-out infinite'
      }
    }
  },
  plugins: []
};
