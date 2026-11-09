import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const workspaceDir = "C:/Users/serva/Documents/dev code/IIoT_simulation";
const skillDir = "C:/Users/serva/.codex/plugins/cache/openai-primary-runtime/presentations/26.1105.12137/skills/presentations";
const outputDir = path.join(workspaceDir, "deliverables");
const buildDir = path.join(workspaceDir, ".codex-slide-build");
const finalPath = path.join(outputDir, "IIoT_Communication_Fault_Diagnosis_Application_Deck_v2.pptx");

const W = 1280, H = 720;
const C = { navy: "#102A43", blue: "#245B84", teal: "#2B7A78", pale: "#F4F7FA", light: "#E8F0F5", ink: "#1F2933", muted: "#52606D", accent: "#D98C2B", red: "#B33A3A" };
const font = "Microsoft JhengHei";

function text(slide, value, x, y, w, h, size = 22, color = C.ink, bold = false, align = "left") {
  const shape = slide.shapes.add({ geometry: "textbox", position: { left: x, top: y, width: w, height: h }, fill: "none", line: { style: "solid", fill: "none", width: 0 } });
  shape.text = value;
  shape.text.style = { fontFamily: font, fontSize: size, color, bold, align, verticalAlign: "middle" };
  return shape;
}
function rect(slide, x, y, w, h, fill, rounded = false) {
  return slide.shapes.add({ geometry: rounded ? "roundRect" : "rect", position: { left: x, top: y, width: w, height: h }, fill, line: { style: "solid", fill, width: 1 }, borderRadius: rounded ? "rounded-md" : undefined });
}
function box(slide, value, x, y, w, h, fill = C.light, color = C.navy) {
  rect(slide, x, y, w, h, fill, true);
  text(slide, value, x + 14, y + 10, w - 28, h - 20, 20, color, true, "center");
}
function arrow(slide, x, y, w = 40, color = C.blue) { text(slide, "→", x, y, w, 36, 28, color, true, "center"); }
function header(slide, title, page) {
  slide.background.fill = "#FFFFFF";
  rect(slide, 0, 0, W, 18, C.navy);
  text(slide, title, 72, 44, 980, 56, 32, C.navy, true);
  text(slide, String(page).padStart(2, "0"), 1140, 50, 68, 38, 16, C.muted, true, "right");
  rect(slide, 72, 112, 1136, 2, "#D9E2EC");
}

async function writeBlob(filePath, blob) { await fs.writeFile(filePath, new Uint8Array(await blob.arrayBuffer())); }

async function main() {
  await fs.mkdir(outputDir, { recursive: true });
  await fs.mkdir(buildDir, { recursive: true });
  const deck = Presentation.create({ slideSize: { width: W, height: H } });

  let s = deck.slides.add();
  s.background.fill = "#F5F8FB"; rect(s, 0, 0, W, 18, C.navy); rect(s, 72, 132, 10, 330, C.teal);
  text(s, "工業物聯網\n通訊故障診斷研究", 110, 132, 720, 160, 46, C.navy, true);
  text(s, "以可重現實驗比較規則、機器學習與 RAG 診斷", 112, 330, 740, 46, 22, C.muted);
  text(s, "碩士在職專班書審簡報", 112, 390, 380, 32, 18, C.teal, true);
  text(s, "申請人：[請填寫姓名]\n申請系所：[請填寫學校及系所]\n日期：2026 年 11 月", 112, 500, 530, 105, 18);
  rect(s, 895, 150, 250, 250, C.navy, true);
  text(s, "Modbus TCP\nOPC UA\nMQTT", 920, 195, 200, 95, 26, "#FFFFFF", true, "center");
  text(s, "Simulator  ·  Dataset  ·  Diagnosis", 895, 320, 250, 34, 14, "#D9E2EC", false, "center");

  s = deck.slides.add(); header(s, "研究動機與核心問題", 2);
  text(s, "製造現場的通訊異常常來自網路延遲、端點中斷、協定設定、認證或資料格式問題。\n單靠人工閱讀日誌，難以跨協定累積可比較的證據。", 72, 150, 1100, 76, 23);
  text(s, "本研究聚焦三個可驗證問題", 72, 266, 500, 34, 22, C.navy, true);
  for (const [n, y, color, q] of [["1",332,C.blue,"不同協定的故障是否能以一致格式保存為 Ground Truth？"],["2",425,C.teal,"規則、ML 與 RAG 在受控資料與獨立情境的診斷表現如何？"],["3",518,C.accent,"未知故障下，系統如何避免高信心卻錯誤的自動判斷？"]]) {
    rect(s, 82, y, 56, 56, color, true); text(s, n, 82, y, 56, 56, 30, "#FFFFFF", true, "center"); text(s, q, 164, y + 4, 970, 50, 23, C.ink, true);
  }
  text(s, "研究取向：可量測、可追溯，並保留成功與失敗案例", 72, 624, 810, 30, 18, C.muted);

  s = deck.slides.add(); header(s, "系統架構與資料流程", 3);
  text(s, "通訊模擬與資料蒐集", 72, 142, 300, 28, 14, C.blue, true);
  [["Modbus TCP\nSimulator",72,"#EAF2F8"],["OPC UA\nSimulator",282,"#EEF5F2"],["MQTT\nBroker",492,"#FFF4E5"],["Collector\nAdapter",702,"#EAF2F8"],["Experiment\nRun",912,"#EEF5F2"]].forEach(([v,x,f],i)=>{box(s,v,x,198,165,92,f); if(i<4) arrow(s,x+169,225);});
  text(s, "每一次實驗保存 ActualFaultType、預測結果、錯誤碼、請求與回應內容。", 72, 330, 1000, 34, 20);
  text(s, "診斷與可追溯性", 72, 405, 260, 28, 14, C.blue, true);
  [["PostgreSQL\nExperiment 資料",72,210,"#EAF2F8"],["規則診斷\n可追溯規則",350,210,"#EEF5F2"],["ML\n分類與信心分數",628,210,"#FFF4E5"],["RAG\n知識來源與處置建議",906,250,"#F5EEF8"]].forEach(([v,x,w,f],i)=>{box(s,v,x,458,w,96,f); if(i<3)arrow(s,x+w+10,489,48);});
  text(s, "Seq 集中保存結構化日誌，CSV 快照與 SHA-256 支持重現。", 72, 606, 1080, 30, 18, C.muted);

  s = deck.slides.add(); header(s, "正式資料集與實驗品質", 4);
  text(s, "正式受控資料快照", 72, 145, 460, 40, 26, C.navy, true);
  [["15","種正常與異常通訊情境",78,C.blue],["1,500","次 Experiment Run",330,C.teal],["3,800","筆可診斷紀錄",600,C.accent]].forEach(([n,label,x,color])=>{text(s,n,x,215,210,78,54,color,true,"center");text(s,label,x,296,210,35,17,C.muted,false,"center");});
  text(s, "資料分割與追溯設計", 72, 402, 500, 40, 26, C.navy, true);
  text(s, "Run-aware split",84,472,290,34,22,C.blue,true); text(s,"以 experiment_run_id 分割訓練與測試，避免同一 Run 同時出現在兩側。",84,510,780,38,19);
  text(s, "CSV coverage + SHA-256",84,575,370,34,22,C.teal,true); text(s,"匯出正式資料快照與覆蓋率，保留資料來源與實驗紀錄。",84,613,780,38,19);
  rect(s,934,174,222,410,C.navy,true); text(s,"受控資料的高準確率\n只能作為方法比較基準\n\n不能直接推論\n真實產線的泛化能力",963,250,164,190,23,"#FFFFFF",true,"center");

  s = deck.slides.add(); header(s, "受控資料上的診斷評估", 5);
  text(s, "規則與 RAG 在受控快照達 100%。ML 的三種模型表現不同，SVM 為 73.08%。", 72, 142, 1070, 40, 21);
  s.charts.add("bar", { position:{left:100,top:220,width:710,height:330}, categories:["Rule","RAG","Logistic Regression","Random Forest","SVM"], series:[{name:"正確率 (%)",values:[100,100,100,100,73.08],fill:C.blue}], hasLegend:false, dataLabels:{showValue:true,position:"outEnd"}, yAxis:{minimumScale:0,maximumScale:110,majorUnit:20,majorGridlines:{style:"solid",fill:"#D9E2EC",width:1}} });
  text(s,"解讀",870,235,200,34,24,C.navy,true); text(s,"規則可追溯，可直接對應錯誤與原因。\n\nRAG 可回查知識來源，輸出處置建議。\n\nML 受控資料的分數不等於未知情境的能力。",870,285,310,242,20);
  text(s,"評估資料：15 種受控情境，1,500 Runs，3,800 筆紀錄",72,625,900,28,17,C.muted);

  s = deck.slides.add(); header(s, "G-01 / G-02：未見 NetworkDelay 的獨立驗證", 6);
  text(s,"研究設計：將 NetworkDelay 排除於原始訓練資料外，再以外部測試資料驗證。",72,142,1080,36,21);
  [["原始 closed-set ML\n未見 NetworkDelay",72,252,"#FDECEC",C.red],["G-01 結果\n3 個模型皆判為 None\n0 / 398 正確",406,270,"#FDECEC",C.red],["納入標註資料\n重新訓練",758,185,"#EAF2F8",C.blue],["G-02 獨立資料\nRandom Forest 388 / 388",1004,204,"#EEF5F2",C.teal]].forEach(([v,x,w,f,c],i)=>{box(s,v,x,230,w,96,f,c); if(i<3)arrow(s,x+w+10,260,i===0?50:42,i===0?C.red:C.blue);});
  text(s,"結論",72,415,180,34,26,C.navy,true); text(s,"ML 無法自動辨識未見故障。取得標註資料後重新訓練，可改善特定獨立資料的表現；\n但仍必須透過接近門檻與其他協定例外情境，繼續檢驗模型的穩定性。",72,466,1090,72,22);
  rect(s,72,592,1136,50,C.pale,true); text(s,"泛化結論以跨條件獨立測試為準，不以單次訓練成功取代驗證。",98,602,1080,28,19,C.muted,true,"center");

  s = deck.slides.add(); header(s, "邊界與未知故障（G-04 至 G-06）", 7);
  text(s,"三組負向案例保留模型失敗結果，用來界定診斷方法的適用範圍。",72,142,1060,36,21);
  text(s,"G-04｜接近門檻的延遲",78,235,360,30,23,C.blue,true); text(s,"NetworkDelay 70–400 ms\nRule / RAG：300 / 500\nRandom Forest：400 / 500",78,276,330,118,20);
  rect(s,450,212,2,240,"#D9E2EC"); text(s,"G-05｜未映射 Exception Code 03",492,235,400,30,23,C.teal,true); text(s,"Adapter 顯示 Unknown\n人工確認護欄：200 / 200\n避免閉集合模型誤分類",492,276,330,118,20);
  rect(s,892,212,2,240,"#D9E2EC"); text(s,"G-06｜ConnectionLost",934,235,270,30,23,C.accent,true); text(s,"TCP reset 對應\nConnectionLost\nSVM 護欄：0 / 100",934,276,230,118,20);
  rect(s,72,520,1136,94,C.navy,true); text(s,"研究發現：Unknown 與人工確認護欄可降低部分錯誤自動判斷；\n不同模型與不同條件的效果不一致，因此不能宣稱單一護欄可全面解決未知故障。",104,538,1080,52,21,"#FFFFFF",true,"center");

  s = deck.slides.add(); header(s, "診斷安全設計：Unknown 與人工確認", 8);
  text(s,"把不確定性顯式保留在輸出中，避免將未知故障硬套到既有分類。",72,142,1090,36,21);
  [["故障訊號\n與 Adapter 觀測",82,205,"#EAF2F8",C.blue],["Rule / ML / RAG\n產生候選診斷",370,220,"#EEF5F2",C.teal],["信心分數\n規則證據\n知識來源",674,205,"#FFF4E5",C.accent],["接受診斷\n或輸出 Unknown",962,210,"#F5EEF8",C.navy]].forEach(([v,x,w,f,c],i)=>{box(s,v,x,250,w,105,f,c); if(i<3)arrow(s,x+w+13,285,52);});
  text(s,"Unknown 觸發條件",82,460,270,32,24,C.navy,true); text(s,"低信心分數、Adapter 未映射例外、診斷證據衝突，都應轉交人工確認。",82,506,1060,36,21);
  text(s,"人工作業可檢視：Ground Truth、原始請求與回應、錯誤碼、日誌與 RAG 知識來源。",82,574,1060,36,20,C.muted);

  s = deck.slides.add(); header(s, "研究貢獻與碩士階段規劃", 9);
  text(s,"目前成果",72,150,450,38,28,C.navy,true); text(s,"建立跨 Modbus TCP、OPC UA、MQTT 的可重現故障診斷平台。\n\n將 Ground Truth、預測結果、知識來源與原始證據分離保存。\n\n以 G-01 至 G-06 呈現模型成功與失敗邊界。",72,208,500,242,22);
  rect(s,632,146,2,410,"#D9E2EC"); text(s,"研究所延伸方向",692,150,440,38,28,C.teal,true); text(s,"擴增不同設備與網路背景的資料，檢驗跨場域穩定性。\n\n研究 Unknown、拒答與成本敏感告警策略，降低錯誤診斷風險。\n\n建立 RAG 解釋品質與人工評閱方法，使建議符合現場排查流程。",692,208,480,242,22);
  rect(s,72,592,1136,52,C.pale,true); text(s,"申請重點：以已完成的系統與可驗證實驗為基礎，延伸為可解釋且可靠的工業通訊診斷研究。",95,604,1090,28,19,C.navy,true,"center");

  const candidatePath = path.join(buildDir, "application-deck-draft.pptx");
  await (await PresentationFile.exportPptx(deck)).save(candidatePath);
  const { finalizePresentation } = await import(pathToFileURL(path.join(skillDir, "container_tools", "artifact_tool_utils.mjs")).href);
  await finalizePresentation({
    explicitTotalSlideCount: 9,
    workspaceDir,
    candidatePath,
    finalPath,
    pythonExecutable: "C:/Program Files/WindowsApps/OpenAI.CodexPrimaryRuntime.v26-1105-1297-0_26.1105.1297.0_x64__3k8sg7r9htsxt/dependencies/python/python.exe",
    integrityValidatorPath: path.join(skillDir, "container_tools", "inspect_presentation_package_integrity.py"),
    layoutValidatorPath: path.join(skillDir, "container_tools", "inspect_presentation_layout_geometry.py"),
    layoutArgs: ["--expected-slide-size-emu", "12192000,6858000", "--validate-bullet-geometry", "--validate-heading-fit"],
    materializeLiteralChartWorkbooks: true,
    fontPolicy: { basis: "design", families: [font, "Arial"], scriptFonts: { ea: "Arial" } },
    verifyArtifactToolImport: true,
    receiptPath: path.join(buildDir, "validation-v2.json"),
  });
  for (let i = 0; i < deck.slides.items.length; i++) {
    const png = await deck.export({ slide: deck.slides.items[i], format: "png", scale: 1 });
    await writeBlob(path.join(buildDir, `slide-${String(i + 1).padStart(2, "0")}.png`), png);
  }
  console.log(finalPath);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
