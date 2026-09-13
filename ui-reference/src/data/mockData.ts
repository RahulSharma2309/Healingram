export type Retreat = {
  id: string;
  name: string;
  location: string;
  state: string;
  price: number;
  rating: number;
  reviews: number;
  duration: string;
  therapies: string[];
  categories: string[];
  problems: string[];
  image: string;
  vendor: string;
  spotsLeft: number;
};

export const retreats: Retreat[] = [
  {
    id: "1",
    name: "Himalayan Mindfulness Retreat",
    location: "Rishikesh",
    state: "Uttarakhand",
    price: 18500,
    rating: 4.8,
    reviews: 124,
    duration: "3 days / 2 nights",
    therapies: ["Meditation", "Yoga", "Breathwork"],
    categories: ["Meditation", "Yoga"],
    problems: ["Stress", "Anxiety", "Burnout"],
    image: "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800&q=80",
    vendor: "Ganga Wellness Ashram",
    spotsLeft: 6,
  },
  {
    id: "2",
    name: "Emotional Healing & Therapy Program",
    location: "Goa",
    state: "Goa",
    price: 32000,
    rating: 4.9,
    reviews: 89,
    duration: "5 days / 4 nights",
    therapies: ["Counselling", "Art Therapy", "Sound Healing"],
    categories: ["Therapy", "Healing"],
    problems: ["Grief", "Trauma", "Relationship stress"],
    image: "https://images.unsplash.com/photo-1545389336-cf090694435e?w=800&q=80",
    vendor: "Coastal Healing Centre",
    spotsLeft: 4,
  },
  {
    id: "3",
    name: "Ayurveda Detox & Rejuvenation",
    location: "Kerala",
    state: "Kerala",
    price: 45000,
    rating: 4.7,
    reviews: 203,
    duration: "7 days / 6 nights",
    therapies: ["Ayurveda", "Panchakarma", "Yoga"],
    categories: ["Ayurveda", "Detox"],
    problems: ["Fatigue", "Digestive issues", "Stress"],
    image: "https://images.unsplash.com/photo-1519823551278-64ac92734fb1?w=800&q=80",
    vendor: "Backwater Ayurveda Resort",
    spotsLeft: 8,
  },
  {
    id: "4",
    name: "Silent Meditation Immersion",
    location: "Dharamshala",
    state: "Himachal Pradesh",
    price: 12000,
    rating: 4.6,
    reviews: 67,
    duration: "4 days / 3 nights",
    therapies: ["Vipassana", "Meditation", "Mindfulness"],
    categories: ["Meditation"],
    problems: ["Anxiety", "Overthinking", "Stress"],
    image: "https://images.unsplash.com/photo-1528319725582-ddc096101511?w=800&q=80",
    vendor: "Mountain Silence Centre",
    spotsLeft: 12,
  },
  {
    id: "5",
    name: "Corporate Burnout Recovery Camp",
    location: "Lonavala",
    state: "Maharashtra",
    price: 22000,
    rating: 4.5,
    reviews: 156,
    duration: "3 days / 2 nights",
    therapies: ["Nature therapy", "Yoga", "Counselling"],
    categories: ["Wellness", "Corporate"],
    problems: ["Burnout", "Stress", "Insomnia"],
    image: "https://images.unsplash.com/photo-1515377901643-e575fe314c78?w=800&q=80",
    vendor: "Hillside Wellness",
    spotsLeft: 10,
  },
  {
    id: "6",
    name: "Trauma-Informed Healing Retreat",
    location: "Rishikesh",
    state: "Uttarakhand",
    price: 28000,
    rating: 4.9,
    reviews: 45,
    duration: "5 days / 4 nights",
    therapies: ["Somatic therapy", "EMDR", "Group therapy"],
    categories: ["Therapy", "Healing"],
    problems: ["Trauma", "PTSD", "Anxiety"],
    image: "https://images.unsplash.com/photo-1593811167562-9cef47bfc4d0?w=800&q=80",
    vendor: "Sacred River Therapy",
    spotsLeft: 3,
  },
];

export const therapyCategories = [
  {
    name: "Meditation",
    count: 48,
    icon: "🧘",
    tagline: "Find stillness and peace of mind",
    image: "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800&q=80",
  },
  {
    name: "Yoga",
    count: 62,
    icon: "🕉️",
    tagline: "Take a break from life and make new friends",
    image: "https://images.unsplash.com/photo-1544367563-1218506f8f74?w=800&q=80",
  },
  {
    name: "Therapy & Counselling",
    count: 34,
    icon: "💚",
    tagline: "Embark on a journey of self-care and growth",
    image: "https://images.unsplash.com/photo-1573497019940-1c28c88b4f3e?w=800&q=80",
  },
  {
    name: "Ayurveda",
    count: 28,
    icon: "🌿",
    tagline: "Ancient healing for mind, body and spirit",
    image: "https://images.unsplash.com/photo-1519823551278-64ac92734fb1?w=800&q=80",
  },
  {
    name: "Sound Healing",
    count: 19,
    icon: "🔔",
    tagline: "Reset your nervous system with vibration",
    image: "https://images.unsplash.com/photo-1511379938547-c1f69419868d?w=800&q=80",
  },
  {
    name: "Detox Programs",
    count: 22,
    icon: "✨",
    tagline: "An internal cleanse to leave you recharged",
    image: "https://images.unsplash.com/photo-1545389336-cf090694435e?w=800&q=80",
  },
];

export const destinations = [
  { name: "Rishikesh", retreats: 42, image: "https://images.unsplash.com/photo-1524492412937-b28c0d8ec9d4?w=600&q=80" },
  { name: "Goa", retreats: 28, image: "https://images.unsplash.com/photo-1512343879784-a960bf40e7f2?w=600&q=80" },
  { name: "Kerala", retreats: 35, image: "https://images.unsplash.com/photo-1600139072354-0d6d0b9e8f3e?w=600&q=80" },
  { name: "Dharamshala", retreats: 18, image: "https://images.unsplash.com/photo-1626621341517-bbf3b603b4e3?w=600&q=80" },
];

export const blogPosts = [
  { id: "1", title: "How to Choose the Right Healing Retreat", excerpt: "A practical guide to matching your wellness goals with the perfect program.", date: "May 10, 2026", category: "Guides" },
  { id: "2", title: "5 Signs You Need a Digital Detox Retreat", excerpt: "Recognize burnout before it becomes chronic.", date: "May 5, 2026", category: "Wellness" },
  { id: "3", title: "Meditation vs Therapy Retreats: What's Right for You?", excerpt: "Compare approaches for stress, anxiety, and emotional healing.", date: "Apr 28, 2026", category: "Therapy" },
];

export const formatPrice = (n: number) =>
  new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(n);
