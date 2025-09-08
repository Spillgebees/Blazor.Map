import { Tooltip } from "leaflet";
import { ISpillgebeesTooltip } from "../interfaces/map";

export const convertToLeafletTooltip = (tooltip: ISpillgebeesTooltip): Tooltip => {
    return new Tooltip({
        content: tooltip.content,
        direction: tooltip.direction,
        offset: tooltip.offset,
        permanent: tooltip.permanent,
        sticky: tooltip.sticky,
        opacity: tooltip.opacity,
        className: tooltip.className
    });
};
