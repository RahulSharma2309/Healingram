import { Link } from "react-router-dom";

export function Signup() {
  return (
    <div className="max-w-md mx-auto px-4 py-16">
      <h1 className="font-display text-2xl font-bold text-sage-800 text-center mb-8">Create your account</h1>
      <form className="bg-white rounded-2xl border border-sand-200 p-6 space-y-4">
        {["Full name", "Email", "Phone", "Password"].map((label) => (
          <label key={label} className="block">
            <span className="text-xs text-gray-500">{label}</span>
            <input className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2" />
          </label>
        ))}
        <Link to="/dashboard" className="block w-full py-3 bg-teal-600 text-white text-center font-medium rounded-xl hover:bg-teal-500">
          Sign up
        </Link>
      </form>
      <p className="text-center text-sm text-gray-500 mt-4">
        Already have an account? <Link to="/login" className="text-teal-600">Log in</Link>
      </p>
    </div>
  );
}
