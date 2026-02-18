import { Tooltip } from "leaflet";
import type { ISpillgebeesTooltip } from "../interfaces/map";

export const convertToLeafletTooltip = (tooltip: ISpillgebeesTooltip): Tooltip => {
  return new Tooltip({
    content: tooltip.content,
    direction: tooltip.direction,
    offset: tooltip.offset ? [tooltip.offset.x, tooltip.offset.y] : [0, 0],
    permanent: tooltip.permanent,
    sticky: tooltip.sticky,
    interactive: tooltip.interactive,
    opacity: tooltip.opacity,
    className: tooltip.className,
  });
};
