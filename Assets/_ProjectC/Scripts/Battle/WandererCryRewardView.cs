using UnityEngine; // 보상 카드 그림과 화면 좌표 사용
using UnityEngine.UI; // 카드 선택용 버튼과 이미지 사용

public static class WandererCryRewardView // 승리 결과 위의 카드와 소유자 선택 화면
{ // 보상 화면 시작
    public static void Show(Transform parent, ExplorationSessionManager session) // 같은 승리의 카드 선택 화면 열기
    { // 화면 생성 시작
        if (parent == null || session == null || !session.WandererCry.HasVictoryChoice || parent.Find("WandererCryReward") != null) // 확정된 승리와 중복 화면 확인
        { // 표시 제외 시작
            return; // 선택 중인 화면 유지
        } // 표시 제외 종료
        string battle = session.ActiveRuntimeEncounterId; // 선택 시작 때의 실제 교전 보관
        var cards = session.WandererCry.GetCards(); // 원래 방에서 저장한 세 후보 조회
        var panel = PreparationUi.CreatePanel("WandererCryReward", parent, Vector2.zero, Vector2.one); // 전투 결과 위의 전체 보상 창
        var canvas = panel.gameObject.AddComponent<Canvas>(); // 보상 전용 정렬 층 생성
        canvas.overrideSorting = true; // 전투 결과 부모와 독립된 모달 정렬
        canvas.sortingOrder = 610; // 레벨과 소모품 및 마이너 카드 선택 화면 위에 표시
        panel.gameObject.AddComponent<GraphicRaycaster>(); // 보상 버튼과 바탕의 입력 우선 처리
        panel.GetComponent<Image>().color = new Color(0.09f, 0.13f, 0.15f, 1f); // 뒤쪽 전투 표시를 가리는 불투명 바탕
        var title = PreparationUi.CreateText("Heading", panel, "방랑자의 울음 · 승리 보상", 38f); // 승리 추가 보상 제목
        PreparationUi.Stretch(title.rectTransform, new Vector2(0.08f, 0.83f), new Vector2(0.92f, 0.94f)); // 상단 제목 영역 지정
        var prompt = PreparationUi.CreateText("Prompt", panel, "공용 카드 한 장을 고른 뒤 받을 생존자를 선택하세요.", 24f); // 두 단계 선택 안내
        PreparationUi.Stretch(prompt.rectTransform, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.82f)); // 안내 문구 영역 지정
        for (int index = 0; index < cards.Count; index++) // 세 후보를 같은 화면에 배치
        { // 개별 카드 표시 시작
            CardData card = cards[index]; // 현재 버튼의 실제 카드 원본
            var button = PreparationUi.CreateButton("Card_" + card.CardId, panel, card.DisplayName, Vector2.zero, Vector2.zero, Vector2.one, () => PickOwner(session, battle, card, panel)); // 후보별 소유자 선택 연결
            float left = 0.075f + index * 0.29f; // 가로 세 칸의 시작 위치 계산
            PreparationUi.Stretch((RectTransform)button.transform, new Vector2(left, 0.16f), new Vector2(left + 0.27f, 0.69f)); // 창 너비에 맞춘 후보 영역 지정
            var label = (RectTransform)button.transform.Find("Label"); // 카드 이름 문구 조회
            PreparationUi.Stretch(label, new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.46f)); // 카드 이름을 삽화 아래에 배치
            var art = new GameObject("Artwork", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); // 실제 카드 삽화 표시
            art.transform.SetParent(button.transform, false); // 선택 버튼에 삽화 연결
            PreparationUi.Stretch(art.rectTransform, new Vector2(0.06f, 0.47f), new Vector2(0.94f, 0.96f)); // 넓은 상단 그림 영역 지정
            art.sprite = card.Artwork; // 실제 카드의 제작 원화 연결
            art.preserveAspect = true; // 카드 원화 가로세로 비율 유지
            art.raycastTarget = false; // 버튼이 그림 위의 입력 수신
            string cost = card.SwordRule == SwordCardRule.Finisher ? "X" : card.ApCost.ToString(); // 가변 비용 카드의 보상 표기
            var details = PreparationUi.CreateText("Details", button.transform, $"AP {cost}\n{card.Description}", 22f); // 실제 비용과 사용 효과 안내
            PreparationUi.Stretch(details.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.33f)); // 아래쪽 카드 설명 영역 지정
        } // 개별 카드 표시 종료
    } // 화면 생성 종료

    private static void PickOwner(ExplorationSessionManager session, string battle, CardData card, RectTransform panel) // 선택한 카드를 받을 생존자 표시
    { // 소유자 선택 시작
        var party = BattleResultManager.EnsureInstance(); // 승리 직후 파티 체력 조회
        ExplorationRewardOwnerView.OpenPartyMember(panel, party.ActiveParty, card.DisplayName, "카드를 받을 생존 파티원을 선택하세요.", "카드 다시 선택", party.IsLivingPartyMember, owner => session.WandererCry.TryClaim(battle, card, owner), owner => Object.Destroy(panel.gameObject)); // 취소하면 같은 후보로 복귀하고 지급하면 승리 결과로 복귀
    } // 소유자 선택 종료
} // 보상 화면 종료
