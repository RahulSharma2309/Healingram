import { useEffect, useState } from "react";
import { fetchContentPage, fetchContentPages, type ContentPage } from "../../lib/api/catalog";

type Props = { title: string; contact?: boolean; faq?: boolean; slug?: string };

export function StaticPage({ title, contact, faq, slug }: Props) {
  const [page, setPage] = useState<ContentPage | null>(null);
  const [faqs, setFaqs] = useState<ContentPage[]>([]);
  const [loadState, setLoadState] = useState<"idle" | "loading" | "ready" | "error">("idle");

  useEffect(() => {
    const pageSlug = slug ?? title.toLowerCase().replace(/\s+/g, "-");
    if (contact) {
      setLoadState("idle");
      return;
    }
    setLoadState("loading");
    if (faq) {
      fetchContentPages("faq")
        .then((items) => {
          setFaqs(items);
          setLoadState("ready");
        })
        .catch(() => setLoadState("error"));
      return;
    }
    fetchContentPage(pageSlug)
      .then((item) => {
        setPage(item);
        setLoadState("ready");
      })
      .catch(() => setLoadState("error"));
  }, [contact, faq, slug, title]);

  return (
    <div className="max-w-3xl mx-auto px-4 py-16">
      <h1 className="font-display text-3xl font-bold text-sage-800 mb-6">{page?.title ?? title}</h1>
      {contact && (
        <form className="bg-white rounded-xl border border-sand-200 p-6 space-y-4">
          {["Name", "Email", "Message"].map((f) => (
            <label key={f} className="block">
              <span className="text-xs text-gray-500">{f}</span>
              {f === "Message" ? (
                <textarea rows={4} className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2" />
              ) : (
                <input className="w-full mt-1 border border-sand-200 rounded-lg px-3 py-2" />
              )}
            </label>
          ))}
          <button type="button" className="px-6 py-2 bg-teal-600 text-white rounded-lg">
            Send message
          </button>
        </form>
      )}
      {faq && loadState === "loading" && <p className="text-sm text-sage-600">Loading FAQs…</p>}
      {faq && loadState === "error" && (
        <p className="text-sm text-red-700">Could not load FAQs from the content API.</p>
      )}
      {faq && loadState === "ready" && (
        <div className="space-y-4">
          {faqs.length === 0 ? (
            <p className="text-sm text-sage-600">No FAQs are published yet.</p>
          ) : (
            faqs.map((item) => (
              <details key={item.slug} className="bg-white rounded-xl border border-sand-200 p-4">
                <summary className="font-medium cursor-pointer">{item.title}</summary>
                <p className="text-gray-600 mt-2 text-sm">{item.body}</p>
              </details>
            ))
          )}
        </div>
      )}
      {!contact && !faq && loadState === "loading" && (
        <p className="text-sm text-sage-600">Loading this page…</p>
      )}
      {!contact && !faq && loadState === "error" && (
        <p className="text-sm text-red-700">Could not load this page from the content API.</p>
      )}
      {!contact && !faq && loadState === "ready" && (
        <p className="text-gray-600 leading-relaxed">{page?.body}</p>
      )}
    </div>
  );
}
