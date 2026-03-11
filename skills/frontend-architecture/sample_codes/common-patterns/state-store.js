import create from "zustand";

// Minimal global store using Zustand
export const useStore = create((set) => ({
	user: null,
	cart: [],
	setUser: (user) => set({ user }),
	addToCart: (item) => set((s) => ({ cart: [...s.cart, item] })),
	clearCart: () => set({ cart: [] }),
}));

// Usage:
// const { user, setUser } = useStore()
