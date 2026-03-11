import { useState } from "react";

/**
 * Modern React Component using Tailwind CSS
 * Demonstrates:
 * - Component structure with Tailwind utilities
 * - Responsive design (sm:, md:, lg: prefixes)
 * - State management with hooks
 * - Accessible form inputs
 * - Dark mode support
 * - Interactive patterns
 */

export function UserProfileCard({
	user = {
		name: "Jane Doe",
		title: "Product Designer",
		image: "https://i.pravatar.cc/300",
	},
}) {
	const [isFollowing, setIsFollowing] = useState(false);
	const [showDetails, setShowDetails] = useState(false);

	return (
		<div className="flex items-center justify-center min-h-screen bg-gradient-to-br from-slate-100 to-slate-200 dark:from-slate-900 dark:to-slate-800 p-4">
			{/* Card Container */}
			<div className="w-full max-w-sm bg-white dark:bg-slate-800 rounded-2xl shadow-xl dark:shadow-2xl overflow-hidden hover:shadow-2xl dark:hover:shadow-3xl transition-shadow duration-300">
				{/* Header Background */}
				<div className="h-32 bg-gradient-to-r from-blue-500 to-indigo-600 relative">
					{/* Profile Image */}
					<img
						src={user.image}
						alt={user.name}
						className="w-24 h-24 rounded-full border-4 border-white dark:border-slate-800 absolute bottom-0 left-6 transform translate-y-1/2 object-cover"
					/>
				</div>

				{/* Content Area */}
				<div className="px-6 pt-16 pb-6">
					{/* Name & Title */}
					<div className="mb-4">
						<h2 className="text-2xl font-bold text-slate-900 dark:text-white">
							{user.name}
						</h2>
						<p className="text-sm text-slate-600 dark:text-slate-400">
							{user.title}
						</p>
					</div>

					{/* Stats */}
					<div className="grid grid-cols-3 gap-4 mb-6 py-4 border-t border-b border-slate-200 dark:border-slate-700">
						<div className="text-center">
							<p className="text-lg font-bold text-slate-900 dark:text-white">
								1.2K
							</p>
							<p className="text-xs text-slate-600 dark:text-slate-400">
								Followers
							</p>
						</div>
						<div className="text-center">
							<p className="text-lg font-bold text-slate-900 dark:text-white">
								340
							</p>
							<p className="text-xs text-slate-600 dark:text-slate-400">
								Following
							</p>
						</div>
						<div className="text-center">
							<p className="text-lg font-bold text-slate-900 dark:text-white">
								24
							</p>
							<p className="text-xs text-slate-600 dark:text-slate-400">
								Posts
							</p>
						</div>
					</div>

					{/* Bio / Details Toggle */}
					<div className="mb-6">
						{!showDetails ? (
							<p className="text-sm text-slate-600 dark:text-slate-400">
								Product designer passionate about creating delightful user
								experiences.
							</p>
						) : (
							<div className="space-y-2 text-sm text-slate-600 dark:text-slate-400">
								<p>📍 San Francisco, CA</p>
								<p>🎨 Design Systems Specialist</p>
								<p>🌐 www.janedoe.com</p>
								<p>📧 jane@example.com</p>
							</div>
						)}
					</div>

					{/* Action Buttons */}
					<div className="flex gap-3 flex-col sm:flex-row">
						<button
							onClick={() => setIsFollowing(!isFollowing)}
							className={`flex-1 px-4 py-2 rounded-lg font-semibold transition-all duration-200 ${
								isFollowing
									? "bg-slate-100 dark:bg-slate-700 text-slate-900 dark:text-white hover:bg-slate-200 dark:hover:bg-slate-600"
									: "bg-blue-600 text-white hover:bg-blue-700 active:scale-95"
							}`}
						>
							{isFollowing ? "✓ Following" : "Follow"}
						</button>

						<button
							onClick={() => setShowDetails(!showDetails)}
							className="flex-1 px-4 py-2 rounded-lg font-semibold bg-slate-100 dark:bg-slate-700 text-slate-900 dark:text-white hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors duration-200"
						>
							{showDetails ? "Hide" : "Details"}
						</button>
					</div>

					{/* Message Button */}
					<button className="w-full mt-3 px-4 py-2 rounded-lg font-semibold border-2 border-slate-300 dark:border-slate-600 text-slate-900 dark:text-white hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors duration-200">
						Message
					</button>
				</div>

				{/* Footer Links */}
				<div className="px-6 py-4 bg-slate-50 dark:bg-slate-900 border-t border-slate-200 dark:border-slate-700 flex gap-4 justify-center text-xs">
					<a
						href="#"
						className="text-blue-600 dark:text-blue-400 hover:underline"
					>
						Portfolio
					</a>
					<a
						href="#"
						className="text-blue-600 dark:text-blue-400 hover:underline"
					>
						Twitter
					</a>
					<a
						href="#"
						className="text-blue-600 dark:text-blue-400 hover:underline"
					>
						LinkedIn
					</a>
				</div>
			</div>
		</div>
	);
}

/**
 * Form Component Example
 */
export function ContactForm() {
	const [formData, setFormData] = useState({
		name: "",
		email: "",
		message: "",
	});
	const [submitted, setSubmitted] = useState(false);

	const handleChange = (e) => {
		setFormData({ ...formData, [e.target.name]: e.target.value });
	};

	const handleSubmit = (e) => {
		e.preventDefault();
		setSubmitted(true);
		setTimeout(() => setSubmitted(false), 3000);
	};

	return (
		<div className="min-h-screen bg-slate-100 dark:bg-slate-900 py-12 px-4">
			<div className="max-w-md mx-auto bg-white dark:bg-slate-800 rounded-lg shadow-lg p-8">
				<h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-6">
					Get in Touch
				</h2>

				{submitted && (
					<div className="mb-6 p-4 bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-100 rounded-lg text-sm">
						✓ Message sent successfully!
					</div>
				)}

				<form onSubmit={handleSubmit} className="space-y-4">
					{/* Name Field */}
					<div>
						<label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
							Name
						</label>
						<input
							type="text"
							name="name"
							value={formData.name}
							onChange={handleChange}
							required
							className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 dark:focus-visible:ring-blue-400 transition-all duration-200"
							placeholder="Your name"
						/>
					</div>

					{/* Email Field */}
					<div>
						<label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
							Email
						</label>
						<input
							type="email"
							name="email"
							value={formData.email}
							onChange={handleChange}
							required
							className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 dark:focus-visible:ring-blue-400 transition-all duration-200"
							placeholder="you@example.com"
						/>
					</div>

					{/* Message Field */}
					<div>
						<label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
							Message
						</label>
						<textarea
							name="message"
							value={formData.message}
							onChange={handleChange}
							required
							className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 dark:focus-visible:ring-blue-400 transition-all duration-200 resize-none"
							placeholder="Your message..."
							rows={4}
						/>
					</div>

					{/* Submit Button */}
					<button
						type="submit"
						className="w-full px-4 py-3 bg-blue-600 text-white font-semibold rounded-lg hover:bg-blue-700 active:scale-95 transition-all duration-200"
					>
						Send Message
					</button>
				</form>
			</div>
		</div>
	);
}
