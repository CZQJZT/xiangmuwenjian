using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JunqiGame.Core;

namespace JunqiGame.UI
{
    public class BoardCellClickHandler : MonoBehaviour, 
        IPointerClickHandler
    {
        private char column;
        private int row;
        private GameUIManager uiManager;

        public void Initialize(char column, int row, GameUIManager uiManager)
        {
            this.column = column;
            this.row = row;
            this.uiManager = uiManager;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (uiManager != null)
            {
                uiManager.HandleCellClick(column, row);
            }
        }
    }
}