# new_debug6 學期修正對定期評量的影響檢查

日期：2026-11-03。本次為工作區程式碼與既有測試紀錄檢查，沒有操作 new_debug6、沒有連線資料庫或執行排名。

## 已知學期問題與證據

依學期 change 的 hotfix-20261103.md：大量學生清單在 UI Automation 查詢離屏儲存格時，UI 執行緒落在 .NET Framework DataGridViewRowAccessibleObject.Bounds 遞迴路徑。修正以自訂資料列覆寫 Bounds，離屏回傳空矩形，套用主清單、人數、分組明細。

該紀錄描述 1admin_new_debug6、114-2、2066 人換載後捲動、上一步、重載、匯出對話框／取消可回應。未獨立核對載入 DLL 雜湊；搜尋輸入未確認，未驗證匯出內容、歷史／Excel 或實際排名。因此不能稱學期完整驗收通過。

## 定期現況與結論

現有 CalculateRegularAssessmentRank.Designer.cs 的 dgvStudentList 是 DevComponents DataGridViewX，使用一般 RowTemplate；CalculateRegularAssessmentRank.cs 約 438～466 行直接 new DataGridViewRow、CreateCells 後 Rows.AddRange。沒有呼叫 MotherGroupConfirmGrid，也沒有 BoundedAccessibleRow 保護。

結論：具備相同基底控制項與一般資料列路徑，存在相同輔助存取卡住風險，但尚未在定期畫面重現，不能宣稱必然發生。AllCells 欄寬還可能增加大量清單成本，但不是已證實的同一根因。

已同步的 MotherGroupConfirmGrid 在 Configure、ConfigurePopulation、ConfigureDetail 呼叫 InstallBoundedAccessibility；新確認 Form 若正確使用這些方法與其填列路徑可沿用修正。單純設定 RowTemplate 不能修正另行 new DataGridViewRow 建立的列，也不追溯修正已存在列。只引用同一份 DLL 不代表舊定期 Grid 自動受到保護。

## 本機與實際環境驗收清單

- [ ] 使用實際 DataGridViewX，以合成至少 2066 人檢查主清單、人數、明細三個 Grid；核對資料列實際型別與建立順序。
- [ ] STA 測試列與核取儲存格 Bounds；離屏空矩形、捲動後位置更新、所有學生／儲存格子項保留，核取 DefaultAction 可用。沿用 MotherGroupConfirmGridAccessibilityCheck 作為參考，記錄執行耗時及門檻。
- [ ] 經使用者指定測試環境，確認實際載入 DLL 版本／路徑與可取得的雜湊，再做跨程序 UI Automation；合成控制項測試不代替此步。
- [ ] 只查必要控制項：載入、手動／自動捲動、上一步、重載、可見搜尋文字與篩選結果、清除搜尋、匯出開啟／取消後再返回。
- [ ] 匯出內容另外驗證完整名單／旗標，不把開啟對話框當作成功匯出。記錄期間、來源、人數、工具、結果，避免輸出學生個資。
- [ ] 若再無回應，保留當時執行緒堆疊與 DLL 資訊，區分 Bounds、欄寬、資料庫或其他路徑；不以延長逾時宣告修復。

## 其他最新學期變更

來源提交 c8b386d 另調整歷史期間預設、批次清單、舊 XML 完整名單與預設旗標、空白／衝突處理。定期新來源尚未實作，目前沒有對應流程可重現；須在 Task 4.3／4.4／4.5 與 Task 5 整合時對照核准規格。特別是 semester-reference.md D05 的空白不參與／提示，與最新學期行為可能不同，不能因共用 helper 已更新而靜默改政策。

本檢查沒有修改產品程式，也沒有把任何畫面／資料庫任務勾為完成。
