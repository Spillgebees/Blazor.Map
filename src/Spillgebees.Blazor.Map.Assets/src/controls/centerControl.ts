import {Control, Map as LeafletMap, LatLng, DomUtil, DomEvent, ControlOptions, Map} from "leaflet";
import {ISpillgebeesCoordinate} from "../interfaces/map";
import { fitToLayers } from "../utils/fitToLayers";

export interface CenterControlOptions extends ControlOptions {
    center: ISpillgebeesCoordinate;
    zoom: number;
    layerIds?: string[] | undefined;
}

export class CenterControl extends Control {
    private map: Map;
    private centerControlOptions: CenterControlOptions;
    private button: HTMLElement | undefined;

    constructor(map: LeafletMap, options: CenterControlOptions) {
        super(options);
        this.map = map;
        this.centerControlOptions = options;
    }

    override onAdd(map: Map): HTMLElement {
        this.map = map;
        const container = DomUtil.create('div', 'leaflet-bar leaflet-control sgb-map-center-control');

        const button = DomUtil.create('a', 'leaflet-control-button sgb-map-center-control-button', container);
        button.title = 'Center map';
        button.href = '#';
        button.setAttribute('role', 'button');
        button.setAttribute('aria-label', 'Center map');

        button.innerHTML = `<svg width="16" height="16" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor">
            <path d="M8 2C4.69 2 2 4.69 2 8s2.69 6 6 6 6-2.69 6-6-2.69-6-6-6zm0 10c-2.21 0-4-1.79-4-4s1.79-4 4-4 4 1.79 4 4-1.79 4-4 4z"/>
            <circle cx="8" cy="8" r="2"/>
            <path d="M8 0v2M8 14v2M0 8h2M14 8h2" stroke="currentColor" stroke-width="1.5" fill="none"/>
        </svg>`;

        this.button = button;

        DomEvent
            .on(button, 'click', DomEvent.stop)
            .on(button, 'click', this.centerView, this);

        return container;
    }

    override onRemove(_: Map) {
        if (this.button) {
            DomEvent.off(this.button, 'click', this.centerView, this);
        }
    }

    centerView() {
        if (!this.map) {
            return;
        }

        if (this.centerControlOptions.layerIds) {
            const layerStorage = window.Spillgebees.Map.layers.get(this.map);
            if (layerStorage) {
                fitToLayers(this.map, layerStorage, this.centerControlOptions.layerIds);
            }
            return;
        }

        if (this.centerControlOptions.center === undefined || this.centerControlOptions.zoom === undefined) {
            return;
        }

        const coordinate = new LatLng(this.centerControlOptions.center.latitude, this.centerControlOptions.center.longitude);
        this.map.setView(coordinate, this.centerControlOptions.zoom);
    }
}

