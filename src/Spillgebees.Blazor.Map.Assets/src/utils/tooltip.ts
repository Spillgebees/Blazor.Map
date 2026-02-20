import { Tooltip } from "leaflet";
import type { ISpillgebeesTooltip } from "../interfaces/map";

export const convertToLeafletTooltip = (tooltip: ISpillgebeesTooltip): Tooltip => {
  return new Tooltip({
    content: tooltip.content,
    ...(tooltip.direction != null && { direction: tooltip.direction }),
    offset: tooltip.offset != null ? [tooltip.offset.x, tooltip.offset.y] : [0, 0],
    permanent: tooltip.permanent,
    sticky: tooltip.sticky,
    interactive: tooltip.interactive,
    ...(tooltip.opacity != null && { opacity: tooltip.opacity }),
    ...(tooltip.className != null && { className: tooltip.className }),
  });
};
