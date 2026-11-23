using TMPro; // 재화 수량 글자 사용
using UnityEngine; // 탐사 화면 배치 사용
using UnityEngine.UI; // 재화 아이콘 이미지 사용

public sealed class ExplorationResourceHud : MonoBehaviour // 탐사 중 보유 재화 표시
{ // 탐사 재화 표시 시작
    private readonly TMP_Text[] values = new TMP_Text[4]; // 골드와 세 가지 재료의 숫자
    private PlayerResourceManager resources; // 실제 보유 재화 관리자

    public static void Create(Canvas canvas, PlayerResourceManager source) // 오른쪽 위 재화 패널 생성
    { // 패널 생성 시작
        RectTransform panel = PreparationUi.CreatePanel("ExplorationResources", canvas.transform, Vector2.one, Vector2.one); // 오른쪽 위 패널 생성
        panel.pivot = Vector2.one; // 패널 오른쪽 위 기준점
        PreparationUi.Place(panel, Vector2.one, new Vector2(-20f, -20f), new Vector2(500f, 56f)); // 화면 경계 안쪽 배치
        Image frame = panel.GetComponent<Image>(); // 네 칸 배경 조회
        LobbyUiArt.Apply(frame, "ExplorationResourceFrame"); // 제작한 재화 테두리 연결
        frame.raycastTarget = false; // 탐사 입력 통과
        ExplorationResourceHud hud = panel.gameObject.AddComponent<ExplorationResourceHud>(); // 재화 화면 연결
        hud.resources = source; // 재화 관리자 보관
        string[] icons = { "Gold", "Screw", "Iron", "Wire" }; // 왼쪽부터 표시할 재화 순서
        for (int i = 0; i < icons.Length; i++) // 네 재화 칸 순회
        { // 개별 재화 칸 생성 시작
            hud.values[i] = hud.CreateEntry(icons[i], i); // 재화 아이콘과 수량 연결
        } // 개별 재화 칸 생성 종료
        source.ResourcesChanged += hud.Refresh; // 획득과 소비 즉시 반영
        hud.Refresh(); // 첫 화면에 현재 수량 표시
    } // 패널 생성 종료

    private TMP_Text CreateEntry(string iconName, int index) // 재화 한 칸 구성
    { // 재화 한 칸 구성 시작
        RectTransform entry = new GameObject(iconName, typeof(RectTransform)).GetComponent<RectTransform>(); // 재화 영역 생성
        entry.SetParent(transform, false); // 네 칸 테두리 안에 연결
        PreparationUi.Stretch(entry, new Vector2(index / 4f, 0f), new Vector2((index + 1) / 4f, 1f)); // 균등한 칸 너비 설정
        Image icon = PreparationUi.CreatePanel("Icon", entry, new Vector2(0.075f, 0.16f), new Vector2(0.39f, 0.84f)).GetComponent<Image>(); // 왼쪽 아이콘 영역
        LobbyUiArt.Apply(icon, iconName); // 로비와 동일한 재화 아이콘 사용
        icon.preserveAspect = true; // 원본 비율 유지
        icon.raycastTarget = false; // 아이콘 입력 통과
        TMP_Text label = PreparationUi.CreateText("Value", entry, string.Empty, 24f); // 수량 글자 생성
        PreparationUi.Stretch(label.rectTransform, new Vector2(0.42f, 0.12f), new Vector2(0.91f, 0.88f)); // 아이콘 오른쪽 숫자 영역
        label.alignment = TextAlignmentOptions.MidlineRight; // 수량 오른쪽 정렬
        label.textWrappingMode = TextWrappingModes.NoWrap; // 큰 수량 한 줄 표시
        label.fontSizeMin = 9f; // 열 자리 수량 축소 표시
        return label; // 갱신할 수량 글자 반환
    } // 재화 한 칸 구성 종료

    private void Refresh() // 재화 변경 시 수량 갱신
    { // 수량 갱신 시작
        values[0].text = resources.Gold.ToString(); // 골드 수량 표시
        values[1].text = resources.Screw.ToString(); // 나사 수량 표시
        values[2].text = resources.IronPlate.ToString(); // 철판 수량 표시
        values[3].text = resources.Wire.ToString(); // 전선 수량 표시
    } // 수량 갱신 종료

    private void OnDestroy() // 탐사 화면 종료 처리
    { // 화면 정리 시작
        if (resources != null) // 연결된 재화 관리자 확인
        { // 재화 알림 해제 시작
            resources.ResourcesChanged -= Refresh; // 종료된 화면의 수량 갱신 해제
        } // 재화 알림 해제 종료
    } // 화면 정리 종료
} // 탐사 재화 표시 종료
