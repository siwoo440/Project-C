using System; // 정확한 문자열 비교와 검증 저장 식별자 사용
using System.Collections; // 실제 플레이 모드 진입 및 복귀 사용
using System.IO; // 테스트 전용 저장 파일 경로 사용
using System.Linq; // 원본 시트 및 회차 카드 순서 비교
using NUnit.Framework; // 원본 에셋과 실제 획득 결과 검사
using UnityEngine; // 유니티 리소스와 저장 위치 사용
using UnityEngine.TestTools; // 실제 플레이 모드의 Unity 검사 등록

public sealed class ProjectCContentAuditTests // 정규 시트 251종과 298종의 실제 전체 경로 검사
{ // 카드와 유물 통합 검사 시작
    [UnityTest] // 실제 획득과 저장을 사용할 플레이 모드 검사
    public IEnumerator AuthoredCatalogAcquiresAndRoundTripsEveryCardAndRelic() // 전체 원본과 저장된 실물 순서 검증
    { // 플레이 모드의 통합 검사 진입 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 원래 저장과 분리된 격리 프로필 확인
        { // 검증 전용 계정 요구 시작
            Assert.Ignore("ProjectCVerification 저장 프로필 필요"); // 검증 프로필에서 실제 게임 자산 복원
        } // 검증 전용 계정 요구 종료
        yield return new EnterPlayMode(); // 기존 관리자와 게임 데이터의 실제 런타임 시작
        VerifyContentRoundTrip(); // 시트 대조와 획득·저장·복원 절차 확인
        yield return new ExitPlayMode(); // 실제 임시 게임 세션 종료
    } // 플레이 모드의 통합 검사 진입 종료

    private static void VerifyContentRoundTrip() // 실제 런타임에서 원본 전부를 동시에 획득하고 복원
    { // 원본 시트와 실제 에셋 검증 시작
        SaveAssetCatalog catalog = Resources.Load<SaveAssetCatalog>("Save/SaveAssetCatalog"); // 게임이 저장 복원에 직접 쓰는 실제 카탈로그
        Assert.IsNotNull(catalog); // 원본 저장 카탈로그의 가져오기 확인
        var cardRows = CardSheetCatalog.Load().Entries; // 실제 플레이에서 사용한 카드 시트의 원본 목록
        var relicRows = RelicSheetCatalog.Load().Entries; // 실제 플레이에서 사용한 유물 시트의 원본 목록
        Assert.AreEqual(251, cardRows.Count); // 도감의 추가 표본과 독립인 실제 원본 카드 수
        Assert.AreEqual(298, relicRows.Count); // 원본 유물 전체 합계
        Assert.AreEqual(251, cardRows.Select(row => row.SourceRow).Distinct().Count()); // 모든 시트 카드 행의 독립성 확인
        Assert.AreEqual(298, relicRows.Select(row => row.SourceRow).Distinct().Count()); // 모든 시트 유물 행의 독립성 확인
        var cards = cardRows.Select(row => ResolveCard(catalog, row)).ToArray(); // 등록된 저장 원본에서 각 시트 카드 확보
        var relics = relicRows.Select(row => ResolveRelic(catalog, row)).ToArray(); // 등록된 저장 원본에서 각 시트 유물 확보
        Assert.AreEqual(cards.Length, cards.Select(card => card.CardId).Distinct(StringComparer.Ordinal).Count()); // 서로 다른 카드를 하나의 저장 ID로 읽지 않는지 확인
        Assert.AreEqual(relics.Length, relics.Select(relic => relic.RelicId).Distinct(StringComparer.Ordinal).Count()); // 서로 다른 유물이 고유 ID로 복원되는지 확인

        var roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 게임의 카드 소유자 후보 명단
        var recovery = CharacterRecoveryManager.EnsureInstance(); // 출전 가능 상태를 제공할 실제 회복 관리자
        var selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(recovery)); // 슬롯을 점유하는 실제 게임 편성 객체
        foreach (var character in roster.Characters.Take(4)) // 원본 소유자 네 명을 사용해 덱 복원 범위 검증
        { // 실제 편성 선택 적용 시작
            Assert.IsTrue(selection.TryAdd(character), character.CharacterId); // 정상 출전 자격과 정식 캐릭터 식별 확인
        } // 실제 편성 선택 적용 종료
        Assert.IsTrue(selection.Validate().CanDeploy && PartyLoadoutRuntime.Prepare(selection)); // 실제 전투 덱과 파티 생성
        var loadout = PartyLoadoutRuntime.Active; // 실제 편성 완료 상태와 시작 덱
        var session = ExplorationSessionManager.EnsureInstance(); // 시드와 현재 탐사 정보를 기록할 실제 관리자
        session.ResetExploration(); // 다른 테스트의 진행을 사용하지 않는 새 탐사 세션
        session.SetCurrentFloorSeed(290251298); // 탐사 저장의 실제 층에서 사용하는 고정 시드
        var deck = RunDeckManager.EnsureInstance(); // 카드 획득을 담당하는 실제 회차 관리자
        deck.ResetToSource(loadout.Deck); // 출정한 네 소유자의 원래 덱 연결
        var inventory = RelicRunManager.EnsureInstance(); // 정식 유물 획득과 중복 보상을 담당
        foreach (var previous in inventory.Inventory.OwnedRelics.ToArray()) // 같은 실행에 남은 검증 유물만 제거
        { // 새 전체 획득 비교 준비 시작
            Assert.IsTrue(inventory.TryRemove(previous.RelicId), previous.RelicId); // 실제 제거로 별도 저장 입력 준비 확인
        } // 새 전체 획득 비교 준비 종료

        int startingCards = deck.CardCount; // 원래 시작 카드와 새 획득 카드의 경계 확인
        for (int index = 0; index < cards.Length; index++) // 시트 순서에 따른 실제 각 카드 획득
        { // 게임 원래 상점과 같은 추가 API 경로 시작
            var owner = loadout.Party.Members[index % loadout.Party.Members.Count]; // 카드의 실제 회차 소유자 순환 선택
            Assert.IsTrue(deck.TryAddCard(cards[index], owner), cards[index].CardId); // 저장 가능한 실제 획득 성공 확인
        } // 게임 원래 상점과 같은 추가 API 경로 종료
        CollectionAssert.AreEqual(cards, deck.Cards.Skip(startingCards).Select(entry => entry.Card)); // 실제 획득한 전체 원본의 고정 순서 확인
        int upgradeIndex = Enumerable.Range(startingCards, cards.Length).First(index => deck.CanUpgradeAt(index)); // 실제 가능한 카드 하나의 강화 결과 보존 확인
        Assert.IsTrue(deck.TryUpgradeAt(upgradeIndex)); // 원래 카드 강화 값을 실제 회차 덱에 등록
        foreach (var relic in relics) // 시트 전체의 실제 획득과 중복 정책 확인
        { // 정상 유물 획득 처리 시작
            Assert.AreEqual(RelicAcquireResult.Acquired, inventory.TryAcquire(relic), relic.RelicId); // 전체 유물을 실제 정상 획득 경로로 등록
            Assert.AreEqual(RelicAcquireResult.DuplicateConverted, inventory.TryAcquire(relic), relic.RelicId); // 실제 중복 변환과 추가되지 않는 보유 개수 확인
        } // 정상 유물 획득 처리 종료
        CollectionAssert.AreEqual(relics, inventory.Inventory.OwnedRelics); // 중복 처리 뒤에도 같은 원본 298종만 보유하는지 확인

        var expectedDeck = DeckRecords(deck); // 획득한 실제 카드와 소유자 및 강화 값의 기준 배열
        string path = Path.Combine(Application.persistentDataPath, "day290-audit-" + Guid.NewGuid().ToString("N") + ".json"); // 실제 사용자 슬롯과 겹치지 않는 테스트 전용 파일
        try // 테스트 예외에도 실제 생성한 파일만 정리
        { // 실제 파일 회차 저장·복원 비교 시작
            Assert.IsTrue(ExplorationSaveService.Save(path, Vector3.zero), "카드·유물 전체 실제 저장"); // 기존 회차 저장 API로 전체 입력 기록
            Assert.IsTrue(ExplorationSaveService.CanLoad(path)); // 같은 파일의 기존 전체 복원 검증 통과 확인
            deck.ResetToSource(loadout.Deck); // 메모리에서 획득한 카드 삭제 후 실제 파일에 의존
            foreach (var relic in relics) // 현재 보유 바이트와 실제 저장 파일의 결과를 분리할 초기화
            { // 유물별 기존 제거 경로 확인 시작
                Assert.IsTrue(inventory.TryRemove(relic.RelicId), relic.RelicId); // 테스트 단계에서 전체 보유 유물 제거
            } // 유물별 기존 제거 경로 확인 종료
            Assert.AreEqual(startingCards, deck.CardCount); // 파일 로드 전에 기존 획득 카드가 없어졌는지 확인
            Assert.IsEmpty(inventory.Inventory.OwnedRelics); // 파일 로드 전에 실제 유물이 초기화되었는지 확인
            Assert.IsTrue(ExplorationSaveService.Load(path), "카드·유물 전체 실제 복원"); // 기존 새 회차 복원 경로에서 저장 내용 로드
            CollectionAssert.AreEqual(expectedDeck, DeckRecords(RunDeckManager.Instance)); // 카드 ID·소유자·강화·순서가 정확히 복원되는지 확인
            CollectionAssert.AreEqual(relics, RelicRunManager.Instance.Inventory.OwnedRelics); // 모든 유물이 실제 원래 참조와 순서대로 로드되는지 확인
            Assert.AreEqual(290251298, ExplorationSessionManager.Instance.CurrentFloorSeed); // 큰 인벤토리 저장에도 같은 현재 탐사 시드 보존
            Debug.Log("[Day290Audit] cardSheet=251 actualAcquired=251 relicSheet=298 actualAcquired=298 duplicates=298 exactSaveRestore=PASS"); // 실제 전체 시트와 복원 결과의 확인 근거
        } // 실제 파일 회차 저장·복원 비교 종료
        finally // 성공 여부와 관계없이 이 검사에서 만든 파일만 제거
        { // 격리 검증 경로 정리 시작
            ExplorationSaveService.Remove(path); // 현재 임시 저장과 그 백업만 제거
        } // 격리 검증 경로 정리 종료
    } // 원본 시트와 실제 에셋 검증 종료

    private static CardData ResolveCard(SaveAssetCatalog catalog, CardSheetEntry row) // 실제 시트 행과 저장에 등록된 원본의 정식 연결
    { // 같은 이름과 속성인 카드 확인 시작
        CardType type = row.Attribute switch // 시트의 다섯 계열을 실제 enum에 대응
        { // 원본 카드 계열의 정확한 변환 시작
            "검" => CardType.Sword, // 검 원본 카드
            "방패" => CardType.Shield, // 방패 원본 카드
            "성배" => CardType.Cup, // 성배 원본 카드
            "지팡이" => CardType.Wand, // 지팡이 원본 카드
            "펜타클" => CardType.Pentacle, // 펜타클 원본 카드
            _ => throw new InvalidOperationException(row.Attribute) // 시트에 잘못된 계열이 있으면 원본 확인
        }; // 원본 카드 계열의 정확한 변환 종료
        var selected = catalog.Cards.Where(card => card != null && card.DisplayName == row.Name && card.CardType == type && card.Artwork != null) // 같은 이름·계열과 실제 원본 원화의 저장 등록 확인
            .OrderByDescending(card => card.CardId.StartsWith("CRD_SHEET_", StringComparison.Ordinal)).FirstOrDefault(); // 여러 원본이 있으면 실제 시트로 등록한 원본 먼저 선택
        Assert.IsNotNull(selected, $"카드 {row.SourceRow}행 {row.Name}의 실제 저장 원본"); // 카드 시트 행과 효과 소스의 유효한 원본 확인
        return selected; // 실제 플레이에서 획득할 원래 카드 객체 반환
    } // 같은 이름과 속성인 카드 확인 종료

    private static RelicData ResolveRelic(SaveAssetCatalog catalog, RelicSheetEntry row) // 정규 시트 유물과 실제 저장 원본 참조 비교
    { // 실제 시트 원래 유물의 중복 확인 시작
        var matches = catalog.Relics.Where(relic => relic != null && relic.DisplayName == row.Name && relic.Icon != null).ToArray(); // 같은 이름과 원화를 가진 실제 저장 자산 선택
        Assert.AreEqual(1, matches.Length, $"유물 {row.SourceRow}행 {row.Name}의 원본 참조 수"); // 원래 한 가지 유물과 정상 원화 연결 확인
        return matches[0]; // 현재 실제 획득에서 사용할 정확한 Unity 자료 반환
    } // 실제 시트 원래 유물의 중복 확인 종료

    private static (string card, string owner, int level)[] DeckRecords(RunDeckManager deck) // 카드와 소유자의 원래 보존 순서 출력
    { // 실제 저장 및 복원 기록 구성 시작
        return deck.Cards.Select(entry => (entry.Card.CardId, entry.Owner.CharacterId, entry.UpgradeLevel)).ToArray(); // 복원 시 수치까지 비교할 고정 순서의 값 배열
    } // 실제 저장 및 복원 기록 구성 종료
} // 카드와 유물 통합 검사 종료
