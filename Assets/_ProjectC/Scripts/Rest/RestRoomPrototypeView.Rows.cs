using TMPro; // 파티와 카드 상태의 표시 글꼴 사용
using UnityEngine; // 상태 영역과 색상 사용
using UnityEngine.UI; // 실제 카드 선택 버튼 사용

public sealed partial class RestRoomPrototypeView // 실제 파티와 카드 행의 표시 구성
{ // 휴식 목록 행 시작
    private void RefreshParty() // 현재 저장 파티에서 체력과 정신력 표시
    { // 파티 목록 갱신 시작
        ClearChildren(partyContent); // 이전 파티 행 제거
        int row = 0; // 첫 번째 파티원 표시 위치
        if (party.ActiveParty != null) // 실제 출전 파티 등록 여부 확인
        { // 출전 파티 조회 시작
            foreach (CharacterData member in party.ActiveParty.Members) // 출전한 파티원의 실제 순서 확인
            { // 파티원 상태 조회 시작
                if (member != null && party.TryGetSavedAllyState(member, out int health, out int mental, out int deaths)) // 유효한 파티원의 저장 수치 조회
                { // 유효한 파티원 표시 시작
                    BuildPartyRow(member, row, health, mental, deaths); // 이름과 회복 수치가 있는 파티 행 추가
                    row++; // 다음 파티원 표시 위치로 진행
                } // 유효한 파티원 표시 종료
            } // 파티원 상태 조회 종료
        } // 출전 파티 조회 종료
        partyEmpty.gameObject.SetActive(row == 0); // 표시할 파티원이 없는 상태 안내
        partyDirty = false; // 목록에 반영한 파티 변경 기록 해제
    } // 파티 목록 갱신 종료

    private void BuildPartyRow(CharacterData member, int position, int health, int mental, int deaths) // 현재 파티원 수치와 초상화 표시
    { // 파티원 행 시작
        bool alive = health > 0; // 현재 저장 HP에서 생존 여부 계산
        RectTransform row = Box($"Member_{position}", partyContent, 0f, 50f * position, 420f, 46f, CardSurface).rectTransform; // 파티원 한 명의 배경 영역
        RectTransform portrait = Box("Portrait", row, 4f, 3f, 41f, 40f, Border).rectTransform; // 기존 파티원 초상화 외곽 배치
        if (member.Portrait != null) // 현재 캐릭터 초상화 연결 확인
        { // 파티원 원화 배치 시작
            DrawArtwork(portrait, member.Portrait, 39f, 38f); // 정사각형 범위의 원본 초상화 표시
        } // 파티원 원화 배치 종료
        Label("MemberName", row, member.DisplayName, 20f, Ink, 54f, 0f, 134f, 25f); // 해당 파티원의 현지화 이름 표시
        string life = alive ? "생존" : "사망"; // 회복 가능 여부를 알리는 생존 문구
        Label("LifeState", row, $"{life} · 사망 {deaths}회", 15f, alive ? Mist : Ember, 54f, 25f, 136f, 17f); // 현재 생존 여부와 저장 사망 횟수 표시
        BuildVital(row, "Health", 199f, health, member.MaxHealth, Ember); // 실제 체력 수치와 막대 표시
        BuildVital(row, "Mental", 309f, mental, BattleMentalRuntime.MaximumMental, Mint); // 실제 정신력 수치와 막대 표시
    } // 파티원 행 종료

    private static void BuildVital(RectTransform parent, string name, float x, int current, int maximum, Color accent) // 파티 생존 수치 한 칸 구성
    { // 생존 수치 표시 시작
        Label(name + "Value", parent, $"{current} / {maximum}", 18f, Ink, x, 2f, 100f, 28f); // 현재 수치와 최대값의 정확한 표시
        Box(name + "Track", parent, x, 33f, 94f, 4f, Surface); // 자원 막대의 빈 범위 표시
        float fill = 94f * Mathf.Clamp01((float)current / Mathf.Max(1, maximum)); // 저장 자원 비율의 표시 너비 계산
        Box(name + "Fill", parent, x, 33f, fill, 4f, accent); // 현재 보유 비율의 색상 막대 표시
    } // 생존 수치 표시 종료

    private CardRow BuildCardRow(int index, RunDeckCardEntry entry, int rowNumber) // 실제 덱 슬롯에 대응하는 선택 행 구성
    { // 강화 후보 행 시작
        Image face = Box($"Card_{index}", cardContent, 0f, RowStep * rowNumber, 651f, 64f, CardSurface); // 원본 덱 위치를 유지하는 카드 영역 생성
        face.raycastTarget = true; // 실제 카드 행을 클릭과 스크롤 입력 대상으로 등록
        Button button = face.gameObject.AddComponent<Button>(); // 정확한 슬롯의 카드 선택 버튼 추가
        button.targetGraphic = face; // 마우스 상태에 따른 행 강조 대상 연결
        button.navigation = new Navigation { mode = Navigation.Mode.None }; // 뒤쪽 탐사 UI로 키보드 선택 이동 차단
        ColorBlock colors = button.colors; // 행의 입력 상태별 색상 조회
        colors.highlightedColor = new Color32(224, 242, 228, 255); // 포인터가 닿은 행의 옅은 강조색
        colors.pressedColor = new Color32(179, 201, 186, 255); // 직접 누르는 카드 행의 강조색
        colors.disabledColor = Color.white; // 별도 잠김 배경과 문구에 맡기는 비활성 색상
        button.colors = colors; // 카드 입력 상태의 색상 적용
        button.onClick.AddListener(() => SelectCard(index, entry)); // 원본 카드와 슬롯을 함께 보존한 선택 연결

        RectTransform thumbnail = Box("Thumbnail", face.transform, 9f, 6f, 62f, 52f, Border).rectTransform; // 카드 원화가 표시될 작은 액자
        if (entry.Card.Artwork != null) // 카드마다 지정된 원화 존재 확인
        { // 카드 전용 그림 배치 시작
            DrawArtwork(thumbnail, entry.Card.Artwork, 60f, 50f); // 카드 원화의 중앙을 작은 액자에 표시
        } // 카드 전용 그림 배치 종료
        else // 지정된 카드 그림 없음 확인
        { // 카드 속성 그림 대체 시작
            Image icon = Box("CardType", thumbnail, 11f, 5f, 40f, 40f, Color.clear); // 카드 종류 아이콘 영역 준비
            BattleUiArt.ApplyCardType(icon, entry.Card.CardType); // 카드 종류에 해당하는 기존 전투 아이콘 표시
        } // 카드 속성 그림 대체 종료

        Label("CardName", face.transform, entry.Card.DisplayName, 24f, Ink, 85f, 5f, 329f, 28f).fontStyle = FontStyles.Bold; // 실제 카드 이름을 첫 줄에 배치
        Label("OwnerName", face.transform, entry.Owner.DisplayName, 17f, Mist, 85f, 34f, 325f, 22f); // 같은 원본의 카드 소유자 표시
        GameObject mark = Box("SelectionMark", face.transform, 0f, 0f, 3f, 64f, Gold).gameObject; // 선택 행의 황동 세로선
        TMP_Text selected = Label("Selected", face.transform, "선택됨", 17f, Gold, 423f, 0f, 95f, 64f, TextAlignmentOptions.Right); // 색상과 별도로 선택 여부 표시
        TMP_Text upgrade = Label("UpgradeLevel", face.transform, string.Empty, 19f, Ink, 526f, 5f, 112f, 29f, TextAlignmentOptions.Right); // 실제 카드 강화 단계 표시 공간
        TMP_Text locked = Label("UpgradeLock", face.transform, string.Empty, 15f, Mist, 526f, 36f, 112f, 21f, TextAlignmentOptions.Right); // 최대 단계 도달 안내 영역
        return new CardRow(index, entry, button, mark, selected, upgrade, locked); // 실제 덱 슬롯과 화면 요소 연결 반환
    } // 강화 후보 행 종료

    private static void ClearChildren(RectTransform parent) // 화면 갱신 전 기존 행 제거
    { // 기존 행 정리 시작
        for (int index = parent.childCount - 1; index >= 0; index--) // 기존 행을 끝부터 순회
        { // 기존 행 한 개 정리 시작
            GameObject child = parent.GetChild(index).gameObject; // 화면 갱신 전에 만들어 둔 이전 행 조회
            child.SetActive(false); // 프레임 종료 전까지 이전 입력과 그림 숨김
            Destroy(child); // 이번 프레임 끝에 사용하지 않는 행 제거
        } // 기존 행 한 개 정리 종료
    } // 기존 행 정리 종료

    private sealed class CardRow // 실제 카드 슬롯과 행의 변경 가능한 표시 연결
    { // 카드 행 연결 시작
        public readonly int Index; // 실제 원본 덱 슬롯 위치
        public readonly RunDeckCardEntry Entry; // 같은 원본 카드를 구분하는 실제 복제본
        public readonly Button Button; // 정확한 슬롯 선택 버튼
        public readonly GameObject SelectionMark; // 선택한 카드의 색상 표시
        public readonly TMP_Text SelectedText; // 선택한 카드의 문자 표시
        public readonly TMP_Text UpgradeText; // 현재 강화 단계 표시
        public readonly TMP_Text LockText; // 최대 강화 상태 표시

        public CardRow(int index, RunDeckCardEntry entry, Button button, GameObject selectionMark, TMP_Text selectedText, TMP_Text upgradeText, TMP_Text lockText) // 실제 덱 슬롯과 행 구성 요소 연결
        { // 행 참조 초기화 시작
            Index = index; // 실제 보유 카드 위치 저장
            Entry = entry; // 시각 카드의 실제 보유 항목 저장
            Button = button; // 카드 입력 대상 저장
            SelectionMark = selectionMark; // 선택 강조선 저장
            SelectedText = selectedText; // 선택 안내 라벨 저장
            UpgradeText = upgradeText; // 카드 강화 라벨 저장
            LockText = lockText; // 최대 단계 라벨 저장
        } // 행 참조 초기화 종료
    } // 카드 행 연결 종료
} // 휴식 목록 행 종료
