using System.Collections.Generic; // 이벤트 목록 사용
using UnityEngine; // 리소스 로드와 스크립터블 오브젝트 사용

public static class ExplorationEventCatalog // 탐사 이벤트 데이터 로더
{
    private static List<ExplorationEventData> cachedEvents; // 캐시된 이벤트 목록

    public static IReadOnlyList<ExplorationEventData> LoadEvents() // 탐사 이벤트 목록 로드
    {
        if (cachedEvents != null)
        {
            return cachedEvents; // 기존 이벤트 캐시 반환
        }

        ExplorationEventData[] loadedEvents =
            Resources.LoadAll<ExplorationEventData>("ExplorationEvents"); // 리소스 이벤트 로드

        cachedEvents =
            new List<ExplorationEventData>(); // 이벤트 캐시 목록 생성

        foreach (ExplorationEventData eventData in loadedEvents)
        {
            if (eventData == null ||
                !eventData.IsValidData())
            {
                continue; // 유효하지 않은 이벤트 제외
            }

            cachedEvents.Add(eventData); // 유효 이벤트 캐시 등록
        }

        if (cachedEvents.Count == 0)
        {
            BuildFallbackEvents(cachedEvents); // 에셋이 없으면 기본 테스트 이벤트 생성
        }

        cachedEvents.Sort(
            (left, right) =>
                string.CompareOrdinal(
                    left.EventId,
                    right.EventId)); // 이벤트 ID 기준 정렬

        return cachedEvents; // 최종 이벤트 목록 반환
    }

    private static void BuildFallbackEvents(
        List<ExplorationEventData> targetEvents) // 기본 테스트 이벤트 생성
    {
        targetEvents.Add(CreateAbandonedCrateEvent()); // 보상형 상자 이벤트 추가
        targetEvents.Add(CreateDamagedTerminalEvent()); // 확률형 단말기 이벤트 추가
        targetEvents.Add(CreateSupplyCacheEvent()); // 선택형 보급품 이벤트 추가
    }

    private static ExplorationEventData CreateAbandonedCrateEvent() // 버려진 상자 이벤트 생성
    {
        ExplorationEventData eventData =
            ScriptableObject.CreateInstance<ExplorationEventData>(); // 런타임 이벤트 오브젝트 생성

        ExplorationEventResourceChange openChange =
            new ExplorationEventResourceChange(); // 상자 열기 보상 생성

        openChange.Initialize(25, 2, 1, 0); // 상자 열기 보상 설정

        ExplorationEventResourceChange ignoreChange =
            new ExplorationEventResourceChange(); // 무시 선택 변화량 생성

        ignoreChange.Initialize(0, 0, 0, 0); // 무시 선택 변화량 설정

        List<ExplorationEventChoiceData> choices =
            new List<ExplorationEventChoiceData>(); // 선택지 목록 생성

        ExplorationEventChoiceData openChoice =
            new ExplorationEventChoiceData(); // 열기 선택지 생성

        openChoice.Initialize(
            "[상자 열기] 골드 +25, 나사 +2, 철판 +1",
            "부서진 상자 안에서 약간의 보급품을 발견했습니다.",
            openChange); // 열기 선택지 초기화

        choices.Add(openChoice); // 열기 선택지 등록

        ExplorationEventChoiceData ignoreChoice =
            new ExplorationEventChoiceData(); // 무시 선택지 생성

        ignoreChoice.Initialize(
            "[무시하기] 변화 없음",
            "굳이 위험을 감수하지 않고 지나쳤습니다.",
            ignoreChange); // 무시 선택지 초기화

        choices.Add(ignoreChoice); // 무시 선택지 등록

        eventData.Initialize(
            "event_abandoned_crate",
            "버려진 상자",
            "먼지 쌓인 통로 구석에서 낡은 상자를 발견했습니다. 뚜껑이 반쯤 열린 채로 방치되어 있으며, 안에는 아직 쓸 만한 자원이 남아 있는 듯합니다.",
            ExplorationEventCategory.Reward,
            null,
            choices); // 상자 이벤트 초기화

        return eventData; // 완성된 이벤트 반환
    }

    private static ExplorationEventData CreateDamagedTerminalEvent() // 손상된 단말기 이벤트 생성
    {
        ExplorationEventData eventData =
            ScriptableObject.CreateInstance<ExplorationEventData>(); // 런타임 이벤트 오브젝트 생성

        ExplorationEventResourceChange successChange =
            new ExplorationEventResourceChange(); // 해킹 성공 보상 생성

        successChange.Initialize(45, 0, 0, 2); // 성공 보상 설정

        ExplorationEventResourceChange failureChange =
            new ExplorationEventResourceChange(); // 해킹 실패 손실 생성

        failureChange.Initialize(-15, 0, 0, 0); // 실패 손실 설정

        ExplorationEventResourceChange leaveChange =
            new ExplorationEventResourceChange(); // 떠나기 변화량 생성

        leaveChange.Initialize(0, 0, 0, 0); // 떠나기 변화량 설정

        List<ExplorationEventChoiceData> choices =
            new List<ExplorationEventChoiceData>(); // 선택지 목록 생성

        ExplorationEventChoiceData hackChoice =
            new ExplorationEventChoiceData(); // 해킹 선택지 생성

        hackChoice.InitializeRandom(
            "[해킹 시도] 성공 65%",
            65,
            "손상된 단말기를 복구해 금고 좌표를 해독했습니다. 보상을 획득했습니다.",
            successChange,
            "단말기가 완전히 꺼지며 작은 폭발을 일으켰습니다. 약간의 골드를 잃었습니다.",
            failureChange); // 확률 선택지 초기화

        choices.Add(hackChoice); // 해킹 선택지 등록

        ExplorationEventChoiceData leaveChoice =
            new ExplorationEventChoiceData(); // 떠나기 선택지 생성

        leaveChoice.Initialize(
            "[떠난다] 변화 없음",
            "쓸모를 판단하기 어렵다고 여겨 그대로 지나쳤습니다.",
            leaveChange); // 떠나기 선택지 초기화

        choices.Add(leaveChoice); // 떠나기 선택지 등록

        eventData.Initialize(
            "event_damaged_terminal",
            "손상된 단말기",
            "벽면에 매립된 오래된 단말기가 간헐적으로 점멸합니다. 전력은 불안정하지만 아직 일부 기능은 살아 있는 것 같습니다.",
            ExplorationEventCategory.Risk,
            null,
            choices); // 단말기 이벤트 초기화

        return eventData; // 완성된 이벤트 반환
    }

    private static ExplorationEventData CreateSupplyCacheEvent() // 보급품 더미 이벤트 생성
    {
        ExplorationEventData eventData =
            ScriptableObject.CreateInstance<ExplorationEventData>(); // 런타임 이벤트 오브젝트 생성

        ExplorationEventResourceChange partsChange =
            new ExplorationEventResourceChange(); // 부품 회수 보상 생성

        partsChange.Initialize(0, 3, 2, 1); // 부품 회수 보상 설정

        ExplorationEventResourceChange sellChange =
            new ExplorationEventResourceChange(); // 골드 교환 보상 생성

        sellChange.Initialize(35, 0, 0, 0); // 골드 교환 보상 설정

        List<ExplorationEventChoiceData> choices =
            new List<ExplorationEventChoiceData>(); // 선택지 목록 생성

        ExplorationEventChoiceData partsChoice =
            new ExplorationEventChoiceData(); // 부품 회수 선택지 생성

        partsChoice.Initialize(
            "[부품 회수] 나사 +3, 철판 +2, 전선 +1",
            "사용 가능한 부품을 차분히 분리해 챙겼습니다.",
            partsChange); // 부품 회수 선택지 초기화

        choices.Add(partsChoice); // 부품 회수 선택지 등록

        ExplorationEventChoiceData sellChoice =
            new ExplorationEventChoiceData(); // 골드 교환 선택지 생성

        sellChoice.Initialize(
            "[가치 높은 부품만 챙기기] 골드 +35",
            "곧바로 현금화하기 좋은 부품만 골라 챙겼습니다.",
            sellChange); // 골드 교환 선택지 초기화

        choices.Add(sellChoice); // 골드 교환 선택지 등록

        eventData.Initialize(
            "event_supply_cache",
            "보급품 더미",
            "무너진 구조물 아래에서 정리되지 않은 보급품 더미를 발견했습니다. 모두 챙기기에는 무거워 보이지만 원하는 방식으로 일부를 가져갈 수 있습니다.",
            ExplorationEventCategory.Choice,
            null,
            choices); // 보급품 이벤트 초기화

        return eventData; // 완성된 이벤트 반환
    }
}
