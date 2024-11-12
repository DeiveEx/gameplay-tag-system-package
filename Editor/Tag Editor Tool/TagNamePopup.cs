using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeiveEx.GameplayTagSystem.Editor
{
    public class TagNamePopup : PopupWindowContent
    {
        private VisualTreeAsset _popupPanel;
        private Action<string> _confirmAction;
        private Button _confirmButton;
        private TextField _input;
        private string _title;

        public override void OnGUI(Rect rect)
        {
            //Intentionally left blank
        }

        public override void OnOpen()
        {
            var root = editorWindow.rootVisualElement;
            _popupPanel.CloneTree(root);

            root.Q<Label>().text = _title;
            _confirmButton = root.Q<Button>();
            _input = root.Q<TextField>();
            
            _confirmButton.clicked += Confirm;

            _input.RegisterValueChangedCallback(_ =>
            {
                ToggleConfirmButton();
            });
            
            _input.RegisterCallback<KeyDownEvent>(OnKeyDown);

            ToggleConfirmButton();
            _input.Focus(); //Doesn't work for some reason... Probably something related to Popup
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(200, 75);
        }

        public void Setup(VisualTreeAsset asset, string title, Action<string> confirmAction)
        {
            _popupPanel = asset;
            _confirmAction = confirmAction;
            _title = title;
        }

        private void ToggleConfirmButton()
        {
            _confirmButton.SetEnabled(!IsInputEmpty());
        }

        private bool IsInputEmpty()
        {
            return string.IsNullOrEmpty(_input.value);
        }
        
        void OnKeyDown(KeyDownEvent ev)
        {
            if (ev.keyCode == KeyCode.Return && !IsInputEmpty())
            {
                Confirm();
            }
        }

        private void Confirm()
        {
            _confirmAction(_input.text);
            editorWindow.Close();
        }
    }
}
