# ADR 0004 — Additive Migration 與保留歷史的轉派回復

狀態：Accepted。日期：2026-11-22。

## 背景

合成需求變更要求主管轉派時留下原因與前後承辦人，重送不能重複紀錄，亦不能破壞既有案件。Baseline 直接編輯 AssignedTo，若保留此入口，新規則可被繞過。

## 方案與決定

採獨立 TransferRequest、Application 的 TransferRules 與 Infrastructure transaction。一般 CaseRules 阻擋既有案件直接更換 AssignedTo，畫面將原承辦欄位鎖定；新案件仍可初次指派。相較於在 Code-behind 混入 SQL，規則可獨立查驗，亦在 transaction 內重新驗證目前資料。

Schema additive：舊案件 backfill policy version=1，新建 default=2，轉派升為2；舊 History 的 Reason/RequestId 保持 null，避免捏造以前不存在的理由。TransferOperations 以 RequestId PK 保存 payload，exact duplicate 回傳成功，不同 payload 回 conflict。rowversion 與鎖保護平行更新。

## 回復選項與取捨

Physical down migration 刪欄位／ledger 會失去新操作，因此不作為安全回復。改以 FeatureFlags 暫停轉派，保留所有 additive schema 與資料，既有查詢／編輯仍可用；migration 重跑不擅自重新啟用功能。這是 **feature rollback**，不是宣稱任意舊 binary 都可安全寫入。尤其 Baseline binary 沒有轉派原因檢查，不應回退後開放其寫入。

## 實際驗證

獨立 temporary DB：注入 migration failure → 確認新增欄位與 version marker 都不存在；套用正確 migration 兩次；舊資料/History 保留、舊 policy=1、新 default=2；執行 rollback SQL 後轉派拒絕，原案件及歷程筆數不變。SQL batch 初稿在新增 RequestId 同批編譯 filtered index 時回 Invalid column name，改為固定字串的延後編譯 DDL；這是實際開發錯誤，並非企業事故。
