using ForestShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        RectTransform productionReceipt;
        readonly long[] receiptCounts = new long[3];
        readonly Text[] receiptValues = new Text[3];
        GameState receiptState;
        float receiptEndsAt;
        void BuildProductionReceiptHud(Transform parent)
        {
            productionReceipt = Rect(parent, "Recent production receipt", 0, 0, 1080, 56);
            productionLabel = Label(productionReceipt, "刚刚", 25, Sage, 28, 0, 120, 56);
            ForestSymbol[] icons = { ForestSymbol.Harvest, ForestSymbol.Workshop, ForestSymbol.Coin };
            for (int i = 0; i < 3; i++)
            {
                float x = 170 + i * 290;
                Symbol(productionReceipt, icons[i], x, 3, 48);
                receiptValues[i] = Label(productionReceipt, "—", 30, Sage, x + 62, 0, 205, 56);
            }
            var touch = productionReceipt.gameObject.AddComponent<Button>(); touch.transition = Selectable.Transition.None;
            productionReceipt.gameObject.AddComponent<Image>().color = Color.clear;
            touch.onClick.AddListener(() => Toast("刚刚 · 收获 " + receiptCounts[0] + "份 · 加工 " + receiptCounts[1] + "件 · 收入 " + receiptCounts[2] + "金币"));
            productionReceipt.gameObject.SetActive(false);
        }
        void RefreshProductionReceipts()
        {
            bool changedState = receiptState != State;
            if (changedState || backgrounded || State.report.pending)
            {
                receiptState = State; previousHarvests = State.harvests; previousCrafts = State.crafts; previousSales = State.sales;
                receiptEndsAt = 0; productionReceipt.gameObject.SetActive(false); productionFlow.gameObject.SetActive(true);
                return;
            }
            long harvest = State.harvests - previousHarvests, craft = State.crafts - previousCrafts, sale = State.sales - previousSales;
            previousHarvests = State.harvests; previousCrafts = State.crafts; previousSales = State.sales;
            if (harvest > 0 || craft > 0 || sale > 0)
            {
                if (Time.unscaledTime >= receiptEndsAt)
                {
                    System.Array.Clear(receiptCounts, 0, receiptCounts.Length);
                    receiptEndsAt = Time.unscaledTime + 2.6f;
                }
                receiptCounts[0] += System.Math.Max(0, harvest); receiptCounts[1] += System.Math.Max(0, craft); receiptCounts[2] += System.Math.Max(0, sale);
                if (harvest > 0) audioController?.Cue("harvest");
                if (craft > 0) audioController?.Cue("cauldron");
                if (sale > 0) audioController?.Cue("sale");
                for (int i = 0; i < 3; i++) receiptValues[i].text = receiptCounts[i] > 0 ? "+" + receiptCounts[i].ToString("N0") : "—";
            }
            bool showing = Time.unscaledTime < receiptEndsAt;
            productionReceipt.gameObject.SetActive(showing); productionFlow.gameObject.SetActive(!showing);
        }
    }
}
