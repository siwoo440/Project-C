using UnityEngine; // 런타임 탐사 오브젝트 탐색 사용

public static class RestRoomPrototypeBootstrap // Prototype 휴식 화면 자동 생성
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TryCreateForExplorationScene() // 탐사 Scene 로드 후 휴식 Prototype 준비
    {
        ExplorationPartyLoadoutProvider provider =
            Object.FindFirstObjectByType<ExplorationPartyLoadoutProvider>(); // 현재 탐사 파티 제공자 조회

        if (provider == null ||
            provider.BattleLoadout == null ||
            provider.BattleLoadout.Deck == null) // 탐사 Scene 여부와 덱 연결 확인
        {
            return; // 비탐사 Scene 생성 중단
        }

        RestRoomRunManager manager = RestRoomRunManager.EnsureInstance(); // 휴식 회차 관리자 준비
        if (!manager.Prepare(provider.BattleLoadout.Deck)) // 현재 탐사 덱 연결 확인
        {
            Debug.LogError("[RestRoom][Day55] 휴식 Prototype 준비에 실패했습니다."); // 준비 실패 로그
            return; // Prototype 생성 중단
        }

        RestRoomPrototypeView existingView =
            Object.FindFirstObjectByType<RestRoomPrototypeView>(); // 기존 휴식 화면 조회

        if (existingView != null) // 중복 화면 확인
        {
            existingView.Initialize(manager); // 기존 화면 관리자 재연결
            return; // 신규 화면 생성 중단
        }

        GameObject viewObject = new GameObject("RestRoomPrototypeView"); // 휴식 Prototype 화면 오브젝트 생성
        RestRoomPrototypeView view = viewObject.AddComponent<RestRoomPrototypeView>(); // 휴식 Prototype 화면 추가
        view.Initialize(manager); // 휴식 관리자 연결
    }
}
