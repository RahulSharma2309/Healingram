import { useEffect, useState } from "react";
import { fetchContentPages, type ContentPage } from "../../lib/api/catalog";

export function Blog() {
  const [posts, setPosts] = useState<ContentPage[]>([]);
  const [source, setSource] = useState<"loading" | "api" | "error">("loading");

  useEffect(() => {
    fetchContentPages("post")
      .then((items) => {
        setPosts(items);
        setSource("api");
      })
      .catch(() => setSource("error"));
  }, []);

  return (
    <div className="max-w-4xl mx-auto px-4 py-10">
      <h1 className="font-display text-3xl font-bold text-sage-800 mb-2">Blog & guides</h1>
      <p className="text-gray-600 mb-10">Published from the content API.</p>
      {source === "loading" && <p className="text-sage-600">Loading…</p>}
      {source === "error" && <p className="text-sage-600">Could not load published posts.</p>}
      <div className="space-y-6">
        {posts.map((post) => (
          <article key={post.slug} className="bg-white rounded-xl border border-sand-200 p-6">
            <h2 className="font-display text-xl font-semibold text-sage-800">{post.title}</h2>
            <p className="text-gray-600 mt-2">{post.body}</p>
          </article>
        ))}
      </div>
    </div>
  );
}
