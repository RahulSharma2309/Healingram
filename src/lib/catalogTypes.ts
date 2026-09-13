export type LaunchProgrammeTheme = string;

export type LaunchRetreat = {
  id: string;
  name: string;
  region: string;
  stateLabel?: string;
  locality: string;
  programmes: LaunchProgrammeTheme[];
  image: string;
  typicalDuration?: string;
  priceFrom?: number | null;
  mvpDemoVerified?: boolean;
};

export type ExploreByNeedCard = {
  id: string;
  label: string;
  description: string;
  image: string;
  iconKey?: string;
  href?: string;
  programmes: LaunchProgrammeTheme[];
};

export type HeroDiscoveryOption = {
  id: string;
  label: string;
  programme: string | null;
};

export function titleFromSlug(slug: string): string {
  return slug
    .split(/[_-]+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

export function programmeThemeLabel(theme: string): string {
  return titleFromSlug(theme);
}

export function getRetreatDisplayTags(retreat: LaunchRetreat, limit = 5): string[] {
  return retreat.programmes.map(programmeThemeLabel).slice(0, limit);
}
