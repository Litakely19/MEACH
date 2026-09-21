const fs = require("fs");
const path = require("path");
const puppeteer = require("puppeteer");

async function main() {
  const htmlPath = path.resolve(__dirname, "SCENARIO_FLOWS.html");
  const pdfPath = path.resolve(__dirname, "SCENARIO_FLOWS.pdf");
  const fileUrl = "file:///" + htmlPath.replace(/\\/g, "/");

  const browser = await puppeteer.launch({
    headless: true,
    args: ["--no-sandbox", "--disable-web-security", "--allow-file-access-from-files"]
  });

  try {
    const page = await browser.newPage();
    await page.goto(fileUrl, { waitUntil: "networkidle0", timeout: 120000 });
    await page.waitForFunction(
      () => document.documentElement.getAttribute("data-mermaid-done") === "1",
      { timeout: 120000 }
    );
    // Extra settle time for SVG layout
    await new Promise((r) => setTimeout(r, 1500));

    await page.pdf({
      path: pdfPath,
      format: "A4",
      printBackground: true,
      margin: { top: "12mm", right: "12mm", bottom: "14mm", left: "12mm" }
    });

    const stats = fs.statSync(pdfPath);
    console.log(`Wrote ${pdfPath} (${stats.size} bytes)`);
  } finally {
    await browser.close();
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
