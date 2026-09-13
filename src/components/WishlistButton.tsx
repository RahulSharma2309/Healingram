import { useEffect, useState } from "react";
import { Heart } from "lucide-react";
import { isWishlisted, subscribeWishlist, toggleWishlist } from "../lib/wishlist";

export function WishlistButton({
  slug,
  className = "absolute top-3 right-3 p-2 bg-white/95 rounded-full hover:bg-white shadow-sm",
}: {
  slug: string;
  className?: string;
}) {
  const [saved, setSaved] = useState(() => isWishlisted(slug));

  useEffect(() => {
    const sync = () => setSaved(isWishlisted(slug));
    sync();
    return subscribeWishlist(sync);
  }, [slug]);

  return (
    <button
      type="button"
      className={className}
      aria-label={saved ? "Remove from wishlist" : "Add to wishlist"}
      aria-pressed={saved}
      onClick={(e) => {
        e.preventDefault();
        e.stopPropagation();
        void toggleWishlist(slug).then(setSaved);
      }}
    >
      <Heart className={`w-4 h-4 ${saved ? "fill-teal-600 text-teal-600" : "text-sage-600"}`} />
    </button>
  );
}
