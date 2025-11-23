using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class UI_MainMenuView : UIPanelBase
{
    [Header("UI References")]
    public Button startButton;
    public Button continueButton;
    public Button settingsButton;
    public Button quitButton;
    
    public override void Initialize()
    {
        startButton.onClick.AddListener(OnStartGame);
        continueButton.onClick.AddListener(OnContinueGame);
        quitButton.onClick.AddListener(OnQuitGame);
    }
    
    public override void OnShow()
    {
        // 检查是否有存档数据
        continueButton.interactable = SaveLoadManagerService.Instance.HasSaveData();
    }
    
    public override void OnBackButton()
    {
        // 主菜单不响应返回键，或者显示退出确认
        UIManagerService.Instance.ShowDialog("退出游戏", "确定要退出游戏吗？", "退出", 
            () => GameManagerService.Instance.QuitGame(), "取消");
    }
    
    private void OnStartGame()
    {
        GameManagerService.Instance.StartNewGame();
    }
    
    private void OnContinueGame()
    {
        GameManagerService.Instance.ContinueGame();
    }
    
    private void OnQuitGame()
    {
        GameManagerService.Instance.QuitGame();
    }
}
