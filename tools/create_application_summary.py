from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_ALIGN_VERTICAL
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "deliverables" / "IIoT_Communication_Fault_Diagnosis_Research_Summary.docx"


def set_font(run, size=10.5, bold=False, color=None):
    run.font.name = "Microsoft JhengHei"
    run._element.rPr.rFonts.set(qn("w:ascii"), "Microsoft JhengHei")
    run._element.rPr.rFonts.set(qn("w:hAnsi"), "Microsoft JhengHei")
    run._element.rPr.rFonts.set(qn("w:eastAsia"), "Microsoft JhengHei")
    run.font.size = Pt(size)
    run.font.bold = bold
    if color:
        run.font.color.rgb = RGBColor(*color)


def remove_title_border(paragraph):
    props = paragraph._p.get_or_add_pPr()
    border = OxmlElement("w:pBdr")
    bottom = OxmlElement("w:bottom")
    bottom.set(qn("w:val"), "nil")
    border.append(bottom)
    props.append(border)


def shade(cell, fill):
    props = cell._tc.get_or_add_tcPr()
    element = OxmlElement("w:shd")
    element.set(qn("w:fill"), fill)
    props.append(element)


def borders(cell):
    props = cell._tc.get_or_add_tcPr()
    element = OxmlElement("w:tcBorders")
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        tag = OxmlElement(f"w:{edge}")
        tag.set(qn("w:val"), "single")
        tag.set(qn("w:sz"), "4")
        tag.set(qn("w:color"), "D9D9D9")
        element.append(tag)
    props.append(element)


def set_cell_text(cell, text, bold=False, color=None, align=WD_ALIGN_PARAGRAPH.LEFT):
    cell.text = ""
    paragraph = cell.paragraphs[0]
    paragraph.alignment = align
    paragraph.paragraph_format.space_after = Pt(2)
    run = paragraph.add_run(text)
    set_font(run, 9.3, bold, color)
    cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
    borders(cell)


def add_table(document, headers, rows, widths):
    table = document.add_table(rows=1, cols=len(headers))
    table.autofit = False
    table.style = "Table Grid"
    for index, header in enumerate(headers):
        cell = table.rows[0].cells[index]
        cell.width = Inches(widths[index])
        shade(cell, "1F4E78")
        set_cell_text(cell, header, True, (255, 255, 255), WD_ALIGN_PARAGRAPH.CENTER)
    for row_index, row in enumerate(rows):
        cells = table.add_row().cells
        for index, value in enumerate(row):
            cells[index].width = Inches(widths[index])
            if row_index % 2:
                shade(cells[index], "F2F7FB")
            alignment = WD_ALIGN_PARAGRAPH.CENTER if index == 0 and len(value) < 14 else WD_ALIGN_PARAGRAPH.LEFT
            set_cell_text(cells[index], value, False, None, alignment)
    document.add_paragraph().paragraph_format.space_after = Pt(2)
    return table


def add_heading(document, text, level=1):
    paragraph = document.add_paragraph()
    paragraph.style = f"Heading {level}"
    paragraph.paragraph_format.space_before = Pt(10 if level == 1 else 6)
    paragraph.paragraph_format.space_after = Pt(4)
    run = paragraph.add_run(text)
    set_font(run, 14 if level == 1 else 11.5, True, (0, 0, 0))


def add_body(document, text):
    paragraph = document.add_paragraph()
    paragraph.paragraph_format.line_spacing = 1.35
    paragraph.paragraph_format.space_after = Pt(5)
    run = paragraph.add_run(text)
    set_font(run, 10.5)


def add_bullets(document, items):
    for item in items:
        paragraph = document.add_paragraph(style="List Bullet")
        paragraph.paragraph_format.space_after = Pt(2)
        paragraph.paragraph_format.line_spacing = 1.2
        run = paragraph.add_run(item)
        set_font(run, 10.2)


def build():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    document = Document()
    section = document.sections[0]
    section.top_margin = Inches(0.65)
    section.bottom_margin = Inches(0.65)
    section.left_margin = Inches(0.72)
    section.right_margin = Inches(0.72)

    styles = document.styles
    styles["Normal"].font.name = "Microsoft JhengHei"
    styles["Normal"]._element.rPr.rFonts.set(qn("w:eastAsia"), "Microsoft JhengHei")

    title = document.add_paragraph(style="Title")
    title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    title.paragraph_format.space_after = Pt(5)
    remove_title_border(title)
    set_font(title.add_run("工業物聯網通訊故障診斷研究專案摘要"), 20, True, (0, 0, 0))
    subtitle = document.add_paragraph()
    subtitle.alignment = WD_ALIGN_PARAGRAPH.CENTER
    subtitle.paragraph_format.space_after = Pt(12)
    set_font(subtitle.add_run("碩士在職專班書審資料"), 12, False, (70, 70, 70))

    info = document.add_paragraph()
    info.alignment = WD_ALIGN_PARAGRAPH.CENTER
    set_font(info.add_run("申請人：[請填寫姓名]    申請系所：[請填寫學校及系所]"), 10)

    add_heading(document, "摘要")
    add_body(document, "本專案建立一套可在 Windows 本機重複執行的工業物聯網通訊故障診斷平台。系統以 Modbus TCP、OPC UA 與 MQTT 模擬工廠設備通訊，從資料蒐集、Ground Truth 標註、資料集匯出，到 Rule-based、Machine Learning 與 RAG 診斷，形成可量測、可追溯、可驗證的研究流程。研究結果顯示，受控資料上的高正確率不能直接代表未知情境的泛化能力；因此本專案另以延遲、未知協定例外與連線中斷進行獨立驗證，並導入 Unknown 與人工確認護欄。")

    add_heading(document, "一 研究動機與問題")
    add_body(document, "智慧製造現場的通訊異常可能源自網路延遲、設備端點、協定設定、認證、資料格式或服務中斷。傳統排查常仰賴人工閱讀錯誤訊息，難以在跨協定情境下累積可重複比較的證據。本研究聚焦三項問題：不同協定的故障特徵是否可被一致保存；Rule-based、ML 與 RAG 的診斷表現及限制為何；以及系統如何避免在未知故障下產生過度自信的錯誤判斷。")

    add_heading(document, "二 系統設計與研究方法")
    add_table(document, ["層面", "實作內容"], [
        ("通訊與模擬", "Modbus TCP、OPC UA、MQTT Adapter；Docker 化 Modbus、Mosquitto、PostgreSQL、Seq；Prosys OPC UA Simulation Server。"),
        ("實驗資料", "Experiment Run 保存實際 Ground Truth、回應時間、錯誤碼、原始請求與回應；ActualFaultType 與 PredictedFaultType 分欄。"),
        ("診斷方法", "Rule-based 提供可追溯規則；RAG 提供知識來源、可能原因與處置建議；ML 使用 Logistic Regression、Random Forest、SVM。"),
        ("資料品質", "ML 以 experiment_run_id 進行 Run-aware split，避免同一 Run 同時出現在訓練與測試。")
    ], [1.15, 5.55])

    add_heading(document, "三 正式資料與受控評估")
    add_body(document, "平台已完成 15 種受控情境的正式資料快照，涵蓋 Modbus TCP、OPC UA 與 MQTT。每一情境皆以 100 Run 為目標，並保存 CSV、覆蓋率與 SHA-256，以維持實驗可重現性。")
    add_table(document, ["項目", "量化成果", "意義"], [
        ("正式資料集", "1,500 Runs，3,800 筆紀錄", "涵蓋 15 種正常與異常通訊情境。"),
        ("Rule-based", "受控快照 100%", "規則、原因與處置可追溯。"),
        ("本機 RAG", "Top-1 / Top-3 100%，平均 7.09 ms", "可引用知識來源與歷史案例。"),
        ("ML 保留測試", "Logistic Regression / Random Forest 100%；SVM 73.08%", "受控資料可作為方法比較基準，但不可直接視為現場泛化。")
    ], [1.2, 2.2, 3.3])

    add_heading(document, "四 泛化與未知故障實驗")
    add_body(document, "為避免僅依受控快照宣稱成效，本研究將 NetworkDelay 與未知協定異常資料獨立於原始訓練資料處理。G-01 至 G-06 的結果同時保留成功與失敗案例，作為模型限制及後續改善的依據。")
    add_table(document, ["實驗", "核心發現"], [
        ("G-01 / G-02", "未見 NetworkDelay 時，三個原始 closed-set ML 模型皆判為 None；固定 Rule/RAG 改善後可辨識高延遲資料。"),
        ("G-03 / G-04", "以 G-01 加入訓練後，Random Forest 在 G-02 為 388 / 388；但近門檻 G-04 降為 400 / 500，顯示不能以單次滿分宣稱泛化。"),
        ("G-05", "未映射 Exception Code 03 會被 Adapter 標示 Unknown；人工確認護欄使三個模型 200 / 200 交由人工確認。"),
        ("G-06", "未受訓的 ConnectionLost 未觸發 Adapter Unknown；信心門檻攔下 Logistic Regression 與 Random Forest 的 100 / 100 筆，但 SVM 攔下 0 / 100。")
    ], [1.35, 5.35])

    add_heading(document, "五 研究貢獻與限制")
    add_bullets(document, [
        "建立涵蓋設備通訊、實驗排程、資料庫、CSV、ML 與 RAG 的完整 IIoT 故障診斷研究平台。",
        "將 Ground Truth、診斷預測、知識來源與原始通訊證據分離保存，支持可追溯與可審查的實驗流程。",
        "以獨立資料揭露 closed-set ML 的強迫分類風險，並驗證 Unknown／人工確認護欄的實際效益與模型差異。",
        "限制包括模擬器資料、單一設備設定與小型保留集；結果不應延伸為真實產線的全面泛化結論。"
    ])

    add_heading(document, "六 後續研究規劃")
    add_body(document, "進入研究所後，我希望從三個方向延伸：第一，納入不同設備、網路背景負載與跨協定資料，檢驗模型穩健性；第二，研究 Unknown、拒答與成本敏感告警策略，降低錯誤診斷風險；第三，建立 RAG 解釋品質與人工評閱機制，使診斷建議更符合現場排查流程。")

    add_heading(document, "申請人自述可用段落")
    add_body(document, "我以工業物聯網通訊故障診斷為題，獨立規劃並實作從協定整合、資料蒐集、故障標註、模型訓練到 RAG 診斷的研究平台。我特別重視實驗 Ground Truth 與預測結果的分離，以及對未知故障的誠實評估。未來希望在研究所中持續探討工業通訊異常的可解釋診斷、泛化能力與實務安全性。")

    add_heading(document, "書審與面試呈現建議")
    add_bullets(document, [
        "以本摘要搭配專案 README，呈現從系統設計、資料蒐集、模型比較到研究限制的完整脈絡。",
        "準備 Docker Desktop、Seq、PostgreSQL、CSV coverage 與 RAG 診斷回應截圖，展示資料從 Simulator 到診斷結果的可追溯流程。",
        "面試時可選擇一個 Modbus 故障情境，說明 Ground Truth、Adapter 觀測、Rule-based、ML 與 RAG 如何產生不同結果。",
        "主動說明 G-04 至 G-06 的負向結果，展現對模型泛化、未知故障與安全風險的理解。"
    ])

    add_heading(document, "提交前請補充的個人資訊")
    add_bullets(document, [
        "現職工作中與設備通訊、資料整合、系統維運或智慧製造相關的具體痛點。",
        "申請系所、研究方向、教師或課程與本研究主題的連結。",
        "個人在系統設計、程式開發、實驗規劃與資料分析中實際負責的工作。"
    ])

    document.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    build()
