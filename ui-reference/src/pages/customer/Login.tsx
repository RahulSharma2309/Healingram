import { Link } from "react-router-dom";
import { logIn } from "../../lib/auth";

export function Login() {
  return (
    <div className="max-w-md mx-auto px-4 py-16">
      <h1 className="font-display text-2xl font-bold text-sage-800 text-center mb-8">Welcome back</h1>
      <form className="bg-white rounded-2xl border border-sand-200 p-6 space-y-4">
        <label className="block">
          <span className="text-xs text-gray-500">Email</span>
          <input type="email" className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2" />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Password</span>
          <input type="password" className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2" />
        </label>
        <Link
          to="/dashboard"
          onClick={() => logIn("Priya")}
          className="block w-full py-3 bg-teal-600 text-white text-center font-medium rounded-xl hover:bg-teal-500"
        >
          Log in
        </Link>
      </form>
      <p className="text-center text-sm text-gray-500 mt-4">
        New here? <Link to="/signup" className="text-teal-600">Create account</Link>
      </p>
    </div>
  );
}
