
using System; // 검사 저장의 고유 이름과 기존 버전 형식 정의
using System.Collections; // 플레이 모드 검사 코루틴
using System.IO; // 검사 전용 저장과 구형 형식 파일 기록
using System.Linq; // 전체 목록의 실제 식별자와 소유자 비교
using NUnit.Framework; // 새 카탈로그와 구형 저장의 대조
using UnityEngine; // 실제 저장 서비스와 원본 리소스
using UnityEngine.TestTools; // 검사 전용 플레이 모드 전환

public sealed class WinterfallPentacleLegacyCatalogTests // 펜타클 확장과 실제 구형 회차 저장 호환 검사
{ // 저장 호환 검사 시작
    [UnityTest] // 원본 버전의 저장 내용을 현재 카탈로그로 복원할 검사
    public IEnumerator Day232SnapshotRestoresOriginalCardAndRelicIds() // 이전 저장의 기존 카드와 유물 및 기본 수치
    { // 이전 저장 플레이 검사 시작
        RequireVerificationProfile(); // 사용자 저장과 분리된 플레이 환경 확인
        yield return new EnterPlayMode(); // 정상 유물과 저장 관리자 생성 모드
        VerifyLegacyIds(); // 오래된 원래 식별자의 전체 저장 및 불러오기 실행
        yield return new ExitPlayMode(); // 검사 전용 회차 종료
    } // 이전 저장 플레이 검사 종료

    [UnityTest] // 실제 카드와 유물의 전체 등록 목록 저장 검증
    public IEnumerator FullSaveCatalogRoundTripsEveryDistinctIdAndOwner() // 큰 목록 전체의 저장 순서와 소유자 복원 비교
    { // 대량 저장 플레이 검사 시작
        RequireVerificationProfile(); // 저장 전용 프로필 확인
        yield return new EnterPlayMode(); // 원래 회차 관리자가 동작하는 모드
        VerifyExpandedCatalog(); // 실제 전체 카탈로그의 저장 및 복원 경로 실행
        yield return new ExitPlayMode(); // 격리된 시험 회차 종료
    } // 대량 저장 플레이 검사 종료

    private static void RequireVerificationProfile() // 이전 버전의 회차 자료를 안전하게 복원할 검사 환경
    { // 검사 실행 조건 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 검사 프로필의 두 필드 검증
        { // 일반 사용자 데이터 경로 분리 시작
            Assert.Ignore("저장이 분리된 ProjectCVerification 검증 사본에서 실행"); // 검증 전용 계정의 저장만 접근
        } // 일반 사용자 데이터 경로 분리 종료
    } // 검사 실행 조건 종료

    private static void VerifyLegacyIds() // 232일차의 실제 외부 필드 모양으로 현재 저장을 투영
    { // 구형 저장 검사 시작
        var (deck, members, session) = Prepare(); // 기존 시작 덱과 파티 및 탐사 시드 준비
        var catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 실제 저장에 등록되는 카드와 유물
        var card = catalog.Cards.Single(item => item.CardId == "CRD_SHEET_THRUST"); // 232일차 이전부터 사용한 실제 찌르기 ID
        var relic = catalog.Relics.Single(item => item.RelicId == "RLC_CRACKED_COMPASS"); // 232일차 이전부터 사용한 실제 나침반 ID
        var relics = RelicRunManager.EnsureInstance(); // 실제 보유 유물 보관 이름
        Assert.IsTrue(deck.TryAddCard(card, members[1])); // 현재 실제 회차 덱에 기존 원본과 둘째 소유자 추가
        Assert.AreEqual(RelicAcquireResult.Acquired, relics.TryAcquire(relic)); // 기존 저장 ID인 유물 실제 획득
        Assert.AreEqual(7, session.GrantPermanentAttackPower(7)); // 새 버전의 별도 값이 현재 저장에 있는 기준 생성
        var save = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 실제 사용자 저장과 분리된 임시 파일
        try // 이전 데이터와 복구본 정리 보장
        { // 이전 구형 형식과 최신 복원 시작
            Assert.IsTrue(ExplorationSaveService.Save(save, Vector3.zero)); // 실제 최신 저장의 ID 자료와 현재 구조 생성
            var full = File.ReadAllText(save); // 새 필드가 실제 들어 있는 정상 회차 파일 읽기
            var old = JsonUtility.ToJson(JsonUtility.FromJson<LegacyRun232>(full)); // 232일차에 존재했던 바깥 필드만 같은 타입으로 기록
            StringAssert.Contains("\"version\":1", old); // 원래 저장 버전 번호 유지 확인
            StringAssert.Contains("\"card\":\"CRD_SHEET_THRUST\"", old); // 구형 카드 원본 ID 유지 확인
            StringAssert.Contains("\"RLC_CRACKED_COMPASS\"", old); // 구형 유물 원본 ID 유지 확인
            StringAssert.DoesNotContain("\"permanentAttackPower\"", old); // 이후 일차의 새 필드가 빠진 실제 구형 입력 확인
            File.WriteAllText(save, old); // 검증용 구형 회차 바이트 기록
            Assert.IsTrue(ExplorationSaveService.CanLoad(save)); // 새 카탈로그가 이전 파일의 모든 식별자를 해석함 확인
            Assert.IsTrue(ExplorationSaveService.Load(save)); // 실제 원본 저장 복원 경로 호출
            Assert.AreSame(card, deck.Cards.Last().Card); // 같은 카드 원본 인스턴스 복원 확인
            Assert.AreSame(members[1], deck.Cards.Last().Owner); // 기존 두 번째 파티원으로 선택 소유자 복원
            Assert.AreSame(relic, relics.Inventory.OwnedRelics.Single()); // 실제 유물 ID 및 원본 복원
            Assert.AreEqual(0, session.PermanentAttackPower); // 구형 데이터에 없던 후속 수치의 올바른 기본값
            Assert.IsTrue(ExplorationSaveService.Save(save, Vector3.zero) && ExplorationSaveService.Load(save)); // 현대 형식으로 다시 저장하고 실제 복원
            Assert.AreSame(card, deck.Cards.Last().Card); // 두 번째 읽기에서도 원본 카드 유지 확인
            Assert.AreSame(relic, relics.Inventory.OwnedRelics.Single()); // 두 번째 읽기에서도 원본 유물 유지 확인
        } // 이전 버전의 실제 복원 종료
        finally // 검증 파일과 복구본 정리 보장
        { // 임시 검사 자료 정리 시작
            ExplorationSaveService.Remove(save); // 현재 테스트에서 만든 파일만 정리
        } // 임시 검사 자료 정리 종료
    } // 구형 저장 검사 종료

    private static void VerifyExpandedCatalog() // 현재 전체 저장 목록을 한 회차로 실제 왕복
    { // 카탈로그 전체 왕복 시작
        var (deck, owners, session) = Prepare(); // 독립 검증 회차 준비
        var source = PartyLoadoutRuntime.Active.Deck; // 실제 저장되는 원본 시작 덱
        var catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 원본 카드와 유물의 전체 카탈로그
        var cards = catalog.Cards.ToArray(); // 실제 저장 순서의 현재 모든 카드 원본
        var relics = catalog.Relics.ToArray(); // 실제 카탈로그 순서의 모든 유물 원본
        Assert.GreaterOrEqual(cards.Length, 250); // 큰 목록 검증 시 일부만 꺼낸 잘못된 카탈로그 방지
        Assert.GreaterOrEqual(relics.Length, 190); // 유물의 대량 확장 원본이 입력에 포함됐는지 확인
        Assert.AreEqual(cards.Length, cards.Select(card => card.CardId).Distinct(StringComparer.Ordinal).Count()); // 정식 카드의 충돌 없는 모든 고정 ID 확인
        Assert.AreEqual(relics.Length, relics.Select(relic => relic.RelicId).Distinct(StringComparer.Ordinal).Count()); // 유물의 충돌 없는 모든 고정 ID 확인
        var roster = cards.Select((card, index) => new RunDeckCardEntry(card, owners[index % owners.Count])).ToArray(); // 모든 원본에 실제 소유자를 번갈아 배정
        deck.RestoreCards(source, roster); // 원래 회차 덱의 전체 원본과 소유자 기록
        var manager = RelicRunManager.EnsureInstance(); // 대량 보유 물품을 저장할 실제 유물 관리자
        foreach (var relic in relics) // 현재 저장에 등록되는 모든 유물 순회
        { // 원본 유물 획득 기록 시작
            Assert.AreEqual(RelicAcquireResult.Acquired, manager.TryAcquire(relic), relic.RelicId); // 정상 신규 획득으로 전체 유물 보관
        } // 원본 유물 획득 기록 종료
        var save = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N") + ".json"); // 격리 프로필의 이 테스트 전용 파일
        try // 실제 대량 저장 파일 정리 보장
        { // 덱과 유물의 전체 원본 복원 시작
            Assert.IsTrue(ExplorationSaveService.Save(save, Vector3.zero)); // 정상 저장 서비스에서 대량 데이터 기록
            session.ResetExploration(); // 이전 메모리의 전체 회차 데이터 초기화
            Assert.IsTrue(ExplorationSaveService.CanLoad(save) && ExplorationSaveService.Load(save)); // 저장된 대량 카드와 유물 및 소유자 복원
            CollectionAssert.AreEqual(roster.Select(entry => entry.Card).ToArray(), deck.Cards.Select(entry => entry.Card).ToArray()); // 모든 카드 원본과 순서 확인
            CollectionAssert.AreEqual(roster.Select(entry => entry.Owner).ToArray(), deck.Cards.Select(entry => entry.Owner).ToArray()); // 모든 카드의 실제 소유자와 순서 확인
            CollectionAssert.AreEqual(relics, manager.Inventory.OwnedRelics); // 모든 유물 원본 및 보유 순서 유지 확인
            Debug.Log($"[PentacleSave269] Full save catalog verified {cards.Length} cards and {relics.Length} relics"); // 실제 검증한 현재 원본 수 기록
        } // 덱과 유물의 전체 원본 복원 종료
        finally // 개별 대량 검사 파일 정리 보장
        { // 현재 임시 자료 정리 시작
            ExplorationSaveService.Remove(save); // 이번 테스트 저장과 복구본만 정리
        } // 현재 임시 자료 정리 종료
    } // 카탈로그 전체 왕복 종료

    private static (RunDeckManager Deck, System.Collections.Generic.IReadOnlyList<CharacterData> Owners, ExplorationSessionManager Session) Prepare() // 정상 탐사 파티와 저장 시드 구성
    { // 독립 검증 탐사 준비 시작
        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 파티 원본 로드
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 출전 자격을 사용하는 실제 새 파티
        Assert.IsTrue(selection.TryAdd(roster.Characters[0]) && selection.TryAdd(roster.Characters[1])); // 두 실제 소유자 후보로 출전 파티 준비
        var session = ExplorationSessionManager.EnsureInstance(); // 현재 실제 탐사 매니저
        session.ResetExploration(); // 이전 테스트와 독립한 회차 시작
        Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 정식 출발 파티와 현재 원본 시작 덱 등록
        var active = PartyLoadoutRuntime.Active; // 저장에 사용될 실제 파티와 덱 조회
        Assert.IsTrue(BattleResultManager.EnsureInstance().RegisterParty(active.Party)); // 실제 생존 아군 상태를 저장에 연결
        session.SetCurrentFloorSeed(269); // 버전 간에 그대로 유지할 실제 층 시드
        var deck = RunDeckManager.EnsureInstance(); // 정상 저장의 현재 회차 덱 관리자 조회
        deck.ResetToSource(active.Deck); // 실제 프로젝트의 원본 출발 덱 연결
        return (deck, active.Party.Members, session); // 독립 시드에 속한 검증 대상
    } // 독립 검증 탐사 준비 종료


    [Serializable] // 탐사 상태 직렬화 지정
    private sealed class LegacyRun232 // 탐사 회차 저장 정보
    { // 탐사 저장 정보 시작
        public int version; // 탐사 저장 형식 버전
        public int encounterPoolVersion; // 탐사 중 고정된 조우 목록 버전
        public int eventPoolVersion; // 현재 층 이벤트 목록 버전과 이전 저장의 기본값 영
        public int floor; // 현재 탐사 층
        public int seed; // 현재 층 생성 시드
        public Vector3 position; // 플레이어 위치
        public string[] party; // 출전 파티 순서
        public ExplorationSaveService.RunCard[] sourceCards; // 탐사 시작 덱 목록
        public ExplorationSaveService.RunCard[] cards; // 현재 회차 덱 목록
        public string[] cleared; // 클리어 조우 목록
        public string[] events; // 처리한 이벤트와 특수 방 목록
        public string pendingMapFragment; // 사용 대기 중인 지도 조각의 원본 이벤트
        public bool usedSealedMap; // 이번 탐사의 봉인된 지도 사용 여부
        public GlassFieldEffect glassFieldEffects; // 다음 교전의 회피와 실명 보관
        public WandererCryRunState wandererCry; // 교전 전 추적과 세 카드 및 귀 막기 보관
        public CrackedCompassRunState compass; // 첫 일반 방 사용 여부와 발견한 사건
        public string[] avoidedEncounters; // 승리 보상 없이 회피한 조우 목록
        public CharredWellRunState well; // 사용자별 물과 현재 및 다음 층 봉인 저장
        public BurntCartRunState cart; // 수레 매복과 상점 할인 저장
        public string[] relics; // 획득 순서 유물 목록
        public string[] consumables; // 소모품 슬롯 목록
        public string shopRoom; // 현재 상점 방 식별자
        public string[] purchasedOffers; // 현재 상점 판매 완료 상품 목록
        public bool restUsed; // 현재 휴식 방 사용 여부
        public int hazardHealth; // 대기 환경 체력 피해
        public int hazardMental; // 대기 환경 정신력 피해
        public float hazardExposure; // 현재 위험 노출도
        public int runExperience; // 탐사 획득 경험치 합계
        public int runGold; // 탐사 획득 골드 합계
        public int runScrew; // 탐사 획득 나사 합계
        public int runIronPlate; // 탐사 획득 철판 합계
        public int runWire; // 탐사 획득 전선 합계
        public bool hasResourceLedger; // 탐사 획득 재화 구분 저장 여부
        public int temporaryGold; // 남은 탐사 획득 골드
        public int temporaryScrew; // 남은 탐사 획득 나사
        public int temporaryIronPlate; // 남은 탐사 획득 철판
        public int temporaryWire; // 남은 탐사 획득 전선
        public string lobbyJson; // 동일 시점 거점 상태
    } // 탐사 저장 정보 종료
} // 저장 호환 검사 종료
