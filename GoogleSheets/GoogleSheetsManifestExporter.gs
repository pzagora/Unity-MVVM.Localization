function doGet() {
  return ContentService
    .createTextOutput(JSON.stringify(buildLocalizationManifest()))
    .setMimeType(ContentService.MimeType.JSON);
}

function buildLocalizationManifest() {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheets = ss.getSheets();

  return {
    spreadsheetId: ss.getId(),
    spreadsheetName: ss.getName(),
    exportedAtUtc: new Date().toISOString(),
    tabs: sheets
      .map(buildTab)
      .filter(Boolean)
  };
}

function buildTab(sheet) {
  const data = sheet.getDataRange().getValues();
  if (data.length < 2 || data[0].length < 2) {
    Logger.log(`Sheet '${sheet.getName()}' skipped: too small or empty.`);
    return null;
  }

  const title = sheet.getName().trim();
  const tableId = getTableId(sheet, title);
  const headers = data[0].map(x => String(x || '').trim());
  const languages = headers.slice(1).map(x => String(x || '').trim().toLowerCase());

  const headerSet = new Set(languages);
  if (headerSet.size !== languages.length) {
    throw new Error(`Duplicate language codes in sheet '${title}'.`);
  }

  const rows = [];
  for (let i = 1; i < data.length; i++) {
    const row = data[i];
    const rawKey = normalizeKey(row[0]);
    if (!rawKey) continue;

    rows.push({
      key: rawKey,
      values: languages.map((_, colIndex) => String(row[colIndex + 1] || '').trim())
    });
  }

  if (rows.length === 0) return null;

  const payloadForChecksum = JSON.stringify({
    title,
    tableId,
    gid: sheet.getSheetId(),
    headers,
    rows
  });

  return {
    title,
    tableId,
    gid: sheet.getSheetId(),
    checksum: sha256Hex(payloadForChecksum),
    headers,
    rows
  };
}

function getTableId(sheet, fallbackTitle) {
  const note = String(sheet.getRange('A1').getNote() || '').trim();
  const match = note.match(/tableId\s*:\s*([^\n\r]+)/i);
  const value = match ? match[1] : fallbackTitle;
  return normalizeKey(value);
}

function normalizeKey(value) {
  return String(value || '')
    .trim()
    .replace(/\\/g, '.')
    .replace(/\//g, '.')
    .toLowerCase();
}

function sha256Hex(input) {
  const bytes = Utilities.computeDigest(
    Utilities.DigestAlgorithm.SHA_256,
    Utilities.newBlob(input, 'application/json').getBytes()
  );

  let out = '';
  for (let i = 0; i < bytes.length; i++) {
    const v = (bytes[i] & 0xFF).toString(16);
    out += v.length === 1 ? '0' + v : v;
  }
  return out;
}
