internal interface IExplorationInteractable // 탐사 F 키로 조작 가능한 오브젝트의 공통 계약
{ // 탐사 상호작용 계약 시작
    bool CanInteract { get; } // 현재 표시하고 실행할 수 있는 상태 확인
    void Interact(ExplorationPlayerController player); // 원래 게임 동작에 실제 플레이어 전달
} // 탐사 상호작용 계약 종료
