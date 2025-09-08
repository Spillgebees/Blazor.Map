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

        button.innerHTML = `<svg width="23" height="22" viewBox="0 0 23 22" xmlns="http://www.w3.org/2000/svg">
            <path d="M11.8618 7C9.65182 7 7.86182 8.79 7.86182 11C7.86182 13.21 9.65182 15 11.8618 15C14.0718 15 15.8618 13.21 15.8618 11C15.8618 8.79 14.0718 7 11.8618 7ZM20.8018 10C20.3418 5.83 17.0318 2.52 12.8618 2.06V0H10.8618V2.06C6.69182 2.52 3.38182 5.83 2.92182 10H0.861816V12H2.92182C3.38182 16.17 6.69182 19.48 10.8618 19.94V22H12.8618V19.94C17.0318 19.48 20.3418 16.17 20.8018 12H22.8618V10H20.8018ZM11.8618 18C7.99182 18 4.86182 14.87 4.86182 11C4.86182 7.13 7.99182 4 11.8618 4C15.7318 4 18.8618 7.13 18.8618 11C18.8618 14.87 15.7318 18 11.8618 18Z"/>
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

    private centerView() {
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

