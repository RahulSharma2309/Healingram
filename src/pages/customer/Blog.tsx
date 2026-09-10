import { blogPosts } from "../../data/mockData";

export function Blog() {
  return (
    <div className="max-w-4xl mx-auto px-4 py-10">
      <h1 className="font-display text-3xl font-bold text-sage-800 mb-2">Blog & guides</h1>
      <p className="text-gray-600 mb-10">SEO content — CMS managed in admin panel</p>
      <div className="space-y-6">
        {blogPosts.map((post) => (
          <article key={post.id} className="bg-white rounded-xl border border-sand-200 p-6 hover:shadow-sm">
            <span className="text-xs font-medium text-teal-600">{post.category}</span>
            <h2 className="font-display text-xl font-semibold text-sage-800 mt-1">{post.title}</h2>
            <p className="text-gray-600 mt-2">{post.excerpt}</p>
            <p className="text-xs text-gray-400 mt-3">{post.date}</p>
          </article>
        ))}
      </div>
    </div>
  );
}
