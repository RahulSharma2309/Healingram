type Props = { title: string; contact?: boolean; faq?: boolean };

const faqs = [
  { q: "How do I book a retreat?", a: "Choose a programme, request availability, and pay only after the retreat confirms." },
  { q: "Can I cancel my booking?", a: "Cancellation terms are confirmed with the retreat when they accept your request." },
  { q: "Is payment secure?", a: "Payment is created on the server and marked paid only after a verified provider event." },
];

export function StaticPage({ title, contact, faq }: Props) {
  return (
    <div className="max-w-3xl mx-auto px-4 py-16">
      <h1 className="font-display text-3xl font-bold text-sage-800 mb-6">{title}</h1>
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
          <button type="button" className="px-6 py-2 bg-teal-600 text-white rounded-lg">Send message</button>
        </form>
      )}
      {faq && (
        <div className="space-y-4">
          {faqs.map((item) => (
            <details key={item.q} className="bg-white rounded-xl border border-sand-200 p-4">
              <summary className="font-medium cursor-pointer">{item.q}</summary>
              <p className="text-gray-600 mt-2 text-sm">{item.a}</p>
            </details>
          ))}
        </div>
      )}
      {!contact && !faq && (
        <p className="text-gray-600 leading-relaxed">
          Healingram.com is India&apos;s dedicated marketplace for wellness retreats, therapy programs, and healing stays.
          Find retreats, practices and people that help you return to yourself. We connect guests with verified retreat partners across meditation, yoga, Ayurveda, and emotional wellness.
        </p>
      )}
    </div>
  );
}

