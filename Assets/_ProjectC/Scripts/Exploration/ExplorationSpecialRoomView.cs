using UnityEngine; // Trigger와 런타임 표시 기능 사용

public sealed class ExplorationSpecialRoomView : MonoBehaviour // 57일차 보물·휴식·상점 방 상호작용
{
    private static Sprite runtimeMarkerSprite; // 특수 방 공용 임시 스프라이트

    private ExplorationMapRuntime mapRuntime; // 현재 탐사 맵 런타임
    private Vector2Int coordinate; // 현재 특수 방 논리 좌표
    private ExplorationRoomType roomType; // 현재 특수 방 역할
    private string runtimeId; // 현재 특수 방 런타임 ID
    private bool waitingForRestUse; // 휴식 UI 사용 완료 감시 여부
    private bool waitingForMarkedPurchase; // 미사용 표식 상점 재방문 표시

    public void Initialize(
        ExplorationMapRuntime runtime,
        Vector2Int roomCoordinate,
        ExplorationRoomType type,
        string roomRuntimeId) // 특수 방 상호작용 초기화
    {
        mapRuntime = runtime; // 탐사 맵 런타임 저장
        coordinate = roomCoordinate; // 방 좌표 저장
        roomType = type; // 방 역할 저장
        runtimeId = roomRuntimeId; // 특수 방 ID 저장
        CreateRoomLabel(); // Prototype 방 역할 문자 표시
    }

    public static Sprite GetRuntimeMarkerSprite() // 특수 방 공용 임시 스프라이트 조회
    {
        if (runtimeMarkerSprite != null) // 기존 임시 스프라이트 확인
        {
            return runtimeMarkerSprite; // 기존 스프라이트 반환
        }

        Texture2D texture = new Texture2D(1, 1); // 단색 임시 텍스처 생성
        texture.name = "Day57SpecialRoomTexture"; // 임시 텍스처 이름 지정
        texture.SetPixel(0, 0, Color.white); // 흰색 원본 픽셀 지정
        texture.Apply(); // 텍스처 변경 적용

        runtimeMarkerSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f); // 단색 사각형 스프라이트 생성

        runtimeMarkerSprite.name = "Day57SpecialRoomSprite"; // 임시 스프라이트 이름 지정
        return runtimeMarkerSprite; // 생성 스프라이트 반환
    }

    public static Color GetRoomColor(ExplorationRoomType type) // 방 역할별 Prototype 색상 조회
    {
        switch (type) // 방 역할 색상 분기
        {
            case ExplorationRoomType.Treasure:
                return new Color(1f, 0.72f, 0.16f, 1f); // 보물 방 주황색 반환

            case ExplorationRoomType.Rest:
                return new Color(0.28f, 0.8f, 0.46f, 1f); // 휴식 방 초록색 반환

            case ExplorationRoomType.Shop:
                return new Color(0.3f, 0.58f, 1f, 1f); // 상점 방 파란색 반환

            default:
                return Color.white; // 기타 방 기본 색상 반환
        }
    }

    private void Update() // 휴식 사용 완료 상태 감시
    {
        if (waitingForMarkedPurchase && !ExplorationSessionManager.EnsureInstance().Cart.IsMarkedShop(runtimeId)) // 표식 상점 첫 결제 완료 확인
        { // 재방문 표시 해제 시작
            waitingForMarkedPurchase = false; // 결제 감시 종료
            gameObject.SetActive(false); // 구매한 일회성 상점 숨김
            return; // 이 방의 상태 갱신 종료
        } // 재방문 표시 해제 종료
        if (!waitingForRestUse) // 휴식 사용 감시 여부 확인
        {
            return; // 감시 불필요 처리
        }

        RestRoomRunManager restManager = RestRoomRunManager.Instance; // 현재 휴식 관리자 조회
        if (restManager == null || !restManager.IsUsed) // 휴식 실행 완료 여부 확인
        {
            return; // 사용 전 감시 유지
        }

        ExplorationSessionManager.EnsureInstance().MarkEventResolved(runtimeId); // 현재 휴식 방 사용 완료 저장
        if (FindFirstObjectByType<ExplorationPlayerController>() != null && !ExplorationAutoSave.SaveCurrentExploration()) // 휴식 결과 자동 저장 확인
        { // 휴식 저장 실패 처리 시작
            Debug.LogWarning("[ExplorationAutoSave] 휴식 결과를 저장하지 못했습니다."); // 기존 저장 유지와 저장 실패 안내
        } // 휴식 저장 실패 처리 종료
        waitingForRestUse = false; // 휴식 사용 감시 종료
        gameObject.SetActive(false); // 사용 완료 휴식 방 표시 숨김
    }

    private void OnTriggerExit2D(Collider2D other) // 플레이어 특수 방 이탈 처리
    {
        ExplorationPlayerController player =
            other.GetComponent<ExplorationPlayerController>(); // 플레이어 이탈 확인

        if (player != null && roomType == ExplorationRoomType.Rest) // 휴식 방 플레이어 이탈 확인
        {
            waitingForRestUse = false; // 다른 위치의 휴식 사용 오인 방지
        }
    }

    private void OnTriggerEnter2D(Collider2D other) // 플레이어 특수 방 접촉 처리
    {
        ExplorationPlayerController player =
            other.GetComponent<ExplorationPlayerController>(); // 플레이어 접촉 확인

        if (player == null) // 플레이어 여부 확인
        {
            return; // 다른 오브젝트 접촉 무시
        }

        ExplorationSessionManager sessionManager = ExplorationSessionManager.EnsureInstance(); // 특수 방 상태 관리자 준비
        if (sessionManager.IsEventResolved(runtimeId)) // 이미 사용한 방 확인
        {
            return; // 재사용 차단
        }

        switch (roomType) // 방 역할별 상호작용 분기
        {
            case ExplorationRoomType.Treasure:
                OpenTreasure(); // 보물 획득 처리
                break;

            case ExplorationRoomType.Rest:
                OpenRestRoom(); // 휴식 방 UI 열기
                break;

            case ExplorationRoomType.Shop:
                OpenShop(); // 상점 UI 열기
                break;
        }
    }

    private void OpenTreasure() // Prototype 랜덤 보물 보상 지급
    {
        ExplorationSessionManager sessionManager = ExplorationSessionManager.EnsureInstance(); // 특수 방 상태 관리자 준비
        if (sessionManager.IsEventResolved(runtimeId)) // 이미 획득한 보물 확인
        { // 중복 보물 방지 시작
            return; // 완료된 보물 재지급 차단
        } // 중복 보물 방지 종료

        if (!ExplorationTreasureRewardService.TryGrantReward(runtimeId, out string message, out ShopOfferData pendingOffer)) // 랜덤 보상 지급 또는 선택 준비 확인
        {
            Debug.LogWarning($"[Exploration][Day57] 보물 보상 지급 실패 - {runtimeId}", this); // 보상 실패 로그
            return; // 실패 시 사용 완료 처리 중단
        }

        if (pendingOffer != null) // 공용 카드 소유자 선택 확인
        { // 소유자 선택 대기 시작
            ExplorationRewardOwnerView.Open(transform, pendingOffer, owner => CompleteCardReward(pendingOffer, owner)); // 현재 파티 소유자 선택 표시
            return; // 선택 전 보물 사용과 저장 보류
        } // 소유자 선택 대기 종료

        CompleteTreasure(message); // 일반 보상 사용 완료 처리
    } // 보물 보상 지급 종료

    private bool CompleteCardReward(ShopOfferData offer, CharacterData owner) // 선택한 소유자에게 보물 카드 지급
    { // 카드 보상 완료 시작
        PartyData party = PartyLoadoutRuntime.Active != null ? PartyLoadoutRuntime.Active.Party : null; // 현재 탐사 파티 조회
        if (ExplorationSessionManager.EnsureInstance().IsEventResolved(runtimeId) || party == null || !party.ContainsCharacter(owner)) // 사용 완료와 파티 소유자 확인
        { // 잘못된 보상 선택 처리 시작
            return false; // 중복 또는 파티 외 지급 차단
        } // 잘못된 보상 선택 처리 종료
        if (!RunDeckManager.EnsureInstance().TryAddCard(offer.Card, owner)) // 실제 회차 덱 카드 추가 확인
        { // 카드 추가 실패 시작
            return false; // 보물 보상 미사용 상태 유지
        } // 카드 추가 실패 종료
        CompleteTreasure($"보물 획득: 카드 [{offer.DisplayName}] / {owner.DisplayName}"); // 지급 후 보물 사용과 저장
        return true; // 카드 보상 지급 성공 반환
    } // 카드 보상 완료 종료

    private void CompleteTreasure(string message) // 보물 사용 완료와 저장 처리
    { // 보물 사용 완료 시작
        ExplorationSessionManager sessionManager = ExplorationSessionManager.EnsureInstance(); // 특수 방 상태 관리자 준비
        sessionManager.MarkEventResolved(runtimeId); // 보물 방 사용 완료 저장
        if (FindFirstObjectByType<ExplorationPlayerController>() != null && !ExplorationAutoSave.SaveCurrentExploration()) // 보물 결과 자동 저장 확인
        { // 보물 저장 실패 처리 시작
            Debug.LogWarning("[ExplorationAutoSave] 보물 결과를 저장하지 못했습니다."); // 기존 저장 유지와 저장 실패 안내
        } // 보물 저장 실패 처리 종료
        Debug.Log($"[Exploration][Day57] {message} / {runtimeId}", this); // 보물 결과 로그
        gameObject.SetActive(false); // 획득 완료 보물 표시 숨김
    } // 보물 사용 완료 종료

    private void OpenRestRoom() // 실제 생성된 휴식 방 열기
    {
        ExplorationSessionManager sessionManager = ExplorationSessionManager.EnsureInstance(); // 휴식 방 사용 상태 관리자 준비

        if (sessionManager.IsEventResolved(runtimeId)) // 현재 휴식 방 사용 완료 여부 확인
        {
            return; // 같은 휴식 방 재사용 차단
        }

        RestRoomRunManager restManager = RestRoomRunManager.EnsureInstance(); // 휴식 회차 관리자 준비
        ExplorationPartyLoadoutProvider provider =
            FindFirstObjectByType<ExplorationPartyLoadoutProvider>(); // 탐사 덱 제공자 조회

        if (provider != null &&
            provider.BattleLoadout != null &&
            provider.BattleLoadout.Deck != null) // 휴식 덱 연결 정보 확인
        {
            restManager.Prepare(provider.BattleLoadout.Deck); // 현재 회차 덱 휴식 시스템 연결
        }

        if (restManager.IsUsed) // 이전 층 휴식 사용 상태 확인
        {
            restManager.ResetPrototypeUsage(); // 새 휴식 방 단위 사용 상태 준비
        }

        RestRoomPrototypeView restView =
            FindFirstObjectByType<RestRoomPrototypeView>(); // 기존 휴식 UI 조회

        if (restView == null) // 휴식 UI 누락 확인
        {
            GameObject viewObject = new GameObject("RestRoomPrototypeView"); // 휴식 UI 오브젝트 생성
            restView = viewObject.AddComponent<RestRoomPrototypeView>(); // 휴식 UI 컴포넌트 추가
            restView.Initialize(restManager); // 휴식 관리자 연결
        }

        int hazardLevel = 0; // 안전 방 기본 위험도
        if (mapRuntime != null &&
            mapRuntime.TryGetHazardAt(
                coordinate,
                out ExplorationHazardRoomState hazardState) &&
            hazardState != null) // 현재 휴식 방 퇴색 위험 존재 여부 확인
        {
            hazardLevel = hazardState.Level; // 실제 퇴색 Lv1~3 적용
        }

        restView.OpenFromRoom(hazardLevel); // 실제 위험도 기반 휴식 UI 열기
        waitingForRestUse = true; // 휴식 실행 완료 감시 시작
    }

    private void OpenShop() // 실제 생성된 일회성 상점 방 열기
    {
        ExplorationSessionManager sessionManager = ExplorationSessionManager.EnsureInstance(); // 상점 사용 상태 관리자 준비

        if (sessionManager.IsEventResolved(runtimeId) && !sessionManager.Cart.IsMarkedShop(runtimeId)) // 미사용 표식 상점의 재입장 허용
        {
            return; // 같은 상점 재방문 차단
        }

        ShopPrototypeView shopView =
            FindFirstObjectByType<ShopPrototypeView>(); // 기존 54일차 상점 UI 조회

        if (shopView == null) // 상점 UI 준비 여부 확인
        {
            Debug.LogWarning("[Exploration][Day57] ShopPrototypeView가 준비되지 않았습니다.", this); // 상점 UI 누락 안내
            return; // 상점 열기 중단
        }

        if (shopView.TryOpenFromRoom(runtimeId)) // 실제 상점 열림 확인
        { // 방문 완료 처리 시작
            sessionManager.MarkEventResolved(runtimeId); // 열린 상점 방문 기록
            waitingForMarkedPurchase = sessionManager.Cart.IsMarkedShop(runtimeId); // 첫 구매 전 재방문 가능 여부 확인
            if (waitingForMarkedPurchase) // 취소하거나 골드가 부족한 표식 상점 유지
            { // 표식 상점 저장 시작
                if (FindFirstObjectByType<ExplorationPlayerController>() != null && !ExplorationAutoSave.SaveCurrentExploration()) // 최초 방문 상점과 미사용 할인 저장 확인
                { // 저장 실패 안내 시작
                    Debug.LogWarning("[ExplorationAutoSave] 표식 상점 방문을 저장하지 못했습니다."); // 기존 저장 보존 안내
                } // 저장 실패 안내 종료
                return; // 첫 상품 결제 전 상점 표식 유지
            } // 표식 상점 저장 종료
            gameObject.SetActive(false); // 이미 방문한 상점 표시 숨김
        } // 방문 완료 처리 종료
    }

    private void CreateRoomLabel() // 특수 방 역할 문자 표시 생성
    {
        GameObject labelObject = new GameObject("RoomTypeLabel", typeof(TextMesh)); // 방 역할 문자 오브젝트 생성
        labelObject.transform.SetParent(transform, false); // 특수 방 표시 하위 배치
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.1f); // 특수 방 중앙 문자 위치 지정

        TextMesh labelText = labelObject.GetComponent<TextMesh>(); // 방 역할 TextMesh 조회
        labelText.text = GetRoomLabel(roomType); // 방 역할 축약 문자 지정
        labelText.anchor = TextAnchor.MiddleCenter; // 문자 중앙 기준 정렬
        labelText.alignment = TextAlignment.Center; // 문자 중앙 정렬
        labelText.fontSize = 60; // 문자 해상도 지정
        labelText.characterSize = 0.08f; // 월드 공간 문자 크기 지정
        labelText.color = Color.white; // 문자 흰색 표시

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>(); // 문자 렌더러 조회
        if (renderer != null) // 문자 렌더러 존재 확인
        {
            renderer.sortingOrder = 5; // 방 사각형보다 앞에 표시
        }
    }

    private static string GetRoomLabel(ExplorationRoomType type) // 방 역할 축약 문자 조회
    {
        switch (type) // 방 역할 문자 분기
        {
            case ExplorationRoomType.Treasure:
                return "T"; // 보물 방 문자 반환

            case ExplorationRoomType.Rest:
                return "R"; // 휴식 방 문자 반환

            case ExplorationRoomType.Shop:
                return "S"; // 상점 방 문자 반환

            default:
                return "?"; // 알 수 없는 방 문자 반환
        }
    }
}
