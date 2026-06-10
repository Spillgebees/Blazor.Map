import { chromium } from "@playwright/test";
const browser = await chromium.launch({ args: ["--enable-unsafe-swiftshader"] });
const page = await browser.newPage();
page.on("console", (m) => { if (m.type() !== "debug") console.log("[console]", m.type(), m.text().slice(0, 200)); });
page.on("pageerror", (e) => console.log("[pageerror]", String(e).slice(0, 300)));
await page.goto("http://127.0.0.1:5012/engine-entity-functional-test");
await page.waitForTimeout(8000);
const info = await page.evaluate(() => {
  const maps = window.Spillgebees?.Map?.maps;
  const entries = maps ? [...maps.entries()] : [];
  return {
    mapCount: entries.length,
    styles: entries.map(([el, m]) => ({ container: el.id || el.className, styleNull: !m.style, removed: m._removed })),
    engineNs: !!window.Spillgebees?.Engine,
    canvas: !!document.querySelector(".sgb-map-container canvas"),
  };
});
console.log(JSON.stringify(info, null, 2));
await browser.close();
