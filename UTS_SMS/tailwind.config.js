/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ["./**/*.{html,cshtml,razor}", "./Views/**/*.{cshtml,razor}", "./Pages/**/*.{cshtml,razor}"],
    theme: {
        extend: {
            fontFamily: {
                'poppins': ['Poppins', 'sans-serif'],
            },
            colors: {
                // Primary Colors (Browns - Trust & Stability)
                'primary': {
                    50: '#FFFAF0',
                    100: '#E8DCC8',
                    200: '#DEB887',
                    300: '#D2691E',
                    400: '#CD853F',
                    500: '#A0522D',
                    600: '#8B4513',
                    700: '#6B4423',
                    800: '#5D3A1A',
                    900: '#3D2314',
                },
                // Accent Colors (Golds - Excellence & Achievement)
                'accent': {
                    100: '#FFCC80',
                    200: '#FFB347',
                    300: '#FFA500',
                    400: '#FFD700',
                    500: '#DAA520',
                    600: '#B8860B',
                },
                // Neutral Colors
                'neutral': {
                    50: '#FDFCFA',
                    100: '#F5F0E8',
                    200: '#D4C4B0',
                    300: '#BBA88A',
                    400: '#A89080',
                    500: '#8B7355',
                    600: '#6B5B4F',
                    700: '#4A4A4A',
                    800: '#2D2D2D',
                    900: '#1A1A1A',
                },
                // Semantic Colors
                'success': {
                    DEFAULT: '#2E7D32',
                    light: '#E8F5E9',
                },
                'warning': {
                    DEFAULT: '#F57C00',
                    light: '#FFF3E0',
                },
                'error': {
                    DEFAULT: '#C62828',
                    light: '#FFEBEE',
                },
                'info': {
                    DEFAULT: '#1565C0',
                    light: '#E3F2FD',
                },
            },
            boxShadow: {
                'sms-sm': '0 2px 8px rgba(139, 69, 19, 0.1)',
                'sms-md': '0 8px 25px rgba(139, 69, 19, 0.15)',
                'sms-lg': '0 15px 40px rgba(139, 69, 19, 0.2)',
                'sms-xl': '0 25px 60px rgba(139, 69, 19, 0.25)',
                'sms-gold': '0 10px 30px rgba(255, 215, 0, 0.3)',
            },
            backgroundImage: {
                'gradient-primary': 'linear-gradient(135deg, #8B4513 0%, #A0522D 50%, #CD853F 100%)',
                'gradient-accent': 'linear-gradient(135deg, #FFD700 0%, #FFA500 50%, #FF8C00 100%)',
                'gradient-warm': 'linear-gradient(135deg, #8B4513 0%, #D2691E 50%, #CD853F 100%)',
                'gradient-gold': 'linear-gradient(135deg, #B8860B 0%, #FFD700 50%, #FFA500 100%)',
                'gradient-subtle': 'none',
                'gradient-dark': 'linear-gradient(135deg, #3D2314 0%, #5D3A1A 100%)',
            },
            borderRadius: {
                'sms': '12px',
            },
        },
    },
    plugins: [],
}