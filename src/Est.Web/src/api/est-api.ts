export interface SessionResponse {
  sessionId: string
  worldId: string
  timelineId: string
  currentTimeSeconds: number
  isPaused: boolean
  planetCount: number
  eventCount: number
  checkpointCount: number
}

export interface AtmosphereResponse {
  surfacePressurePascals: number
  compositionByMoleFraction: Record<string, number>
}

export interface PlanetEnvironmentResponse {
  meanSurfaceTemperatureKelvin: number
  surfaceWaterFraction: number
  iceCoverageFraction: number
  atmosphere: AtmosphereResponse
}

export interface PlanetResponse {
  planetId: string
  name: string
  massKilograms: number
  meanRadiusMeters: number
  surfaceGravityMetersPerSecondSquared: number
  environment: PlanetEnvironmentResponse
}

export interface PopulationPersonResponse {
  personId: string
  planetId: string
  sex: string
  birthTimeSeconds: number
  latitudeDegrees: number
  longitudeDegrees: number
  parentId: string | null
  isPregnant: boolean
  pregnancyConceptionTimeSeconds: number | null
  pregnancyFatherId: string | null
  activity: string
  energyReserve: number
  health: number
}

export interface AnimalResponse {
  animalId: string
  planetId: string
  species: string
  birthTimeSeconds: number
  parentId: string | null
  sex: string | null
  isPregnant: boolean
  pregnancyConceptionTimeSeconds: number | null
  pregnancyFatherId: string | null
  latitudeDegrees: number
  longitudeDegrees: number
  energyReserve: number
  health: number
  activity: string
  material: {
    liveBiomassKilograms: number
    liveNitrogenKilograms: number
  }
}

export interface WorldResponse {
  worldId: string
  currentTimeSeconds: number
  planets: PlanetResponse[]
  population: PopulationPersonResponse[]
  animals: AnimalResponse[]
}

export interface SurfaceGridResponse {
  kind: string
  identityVersion: number
  latitudeBandCount: number
  longitudeBandCount: number
}

export interface SurfaceCoordinateResponse {
  latitudeDegrees: number
  longitudeDegrees: number
}

export interface SurfaceCellResponse {
  cellId: string
  centerLatitudeDegrees: number
  centerLongitudeDegrees: number
  areaSquareMeters: number
  boundary: SurfaceCoordinateResponse[]
}

export interface SurfaceResponse {
  planetId: string
  grid: SurfaceGridResponse
  cells: SurfaceCellResponse[]
}

export interface TerrainCellResponse {
  cellId: string
  elevationMeters: number
}

export interface TerrainResponse {
  planetId: string
  grid: SurfaceGridResponse
  cells: TerrainCellResponse[]
}

export interface HydrologyCellResponse {
  cellId: string
  atmosphericWaterKilogramsPerSquareMeter: number
  surfaceLiquidWaterKilogramsPerSquareMeter: number
  soilWaterKilogramsPerSquareMeter: number
  snowIceWaterEquivalentKilogramsPerSquareMeter: number
}

export interface HydrologyResponse {
  planetId: string
  grid: SurfaceGridResponse
  totalWaterMassKilograms: number
  cells: HydrologyCellResponse[]
}

export interface StandingWaterCellResponse {
  cellId: string
  kind: string
  waterBodyAnchorCellId: string | null
  waterDepthMeters: number
  waterSurfaceElevationMeters: number
}

export interface StandingWaterBodyResponse {
  anchorCellId: string
  kind: string
  cellIds: string[]
  surfaceAreaSquareMeters: number
  waterVolumeCubicMeters: number
}

export interface StandingWaterResponse {
  planetId: string
  grid: SurfaceGridResponse
  cells: StandingWaterCellResponse[]
  waterBodies: StandingWaterBodyResponse[]
}

export interface TimelineEventResponse {
  eventId: string
  occurredAtSeconds: number
  cause: string
  summary: string
  affectedPlanetId: string | null
  elapsedSeconds: number
  metrics: Record<string, number>
}

export interface TimelineResponse {
  timelineId: string
  worldId: string
  parentTimelineId: string | null
  parentCheckpointId: string | null
  currentTimeSeconds: number
  checkpoints: Array<{
    checkpointId: string
    timeSeconds: number
  }>
  events: TimelineEventResponse[]
}

export class EstApi {
  private readonly baseUrl: string

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl
  }

  private async get<T>(path: string): Promise<T> {
    const response = await fetch(`${this.baseUrl}${path}`)

    if (!response.ok) {
      throw new Error(`Est API request failed: ${response.status}`)
    }

    return response.json() as Promise<T>
  }

  private async post<T>(
    path: string,
    body: unknown,
  ): Promise<T> {
    const response = await fetch(
      `${this.baseUrl}${path}`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(body),
      },
    )

    if (!response.ok) {
      throw new Error(`Est API request failed: ${response.status}`)
    }

    return response.json() as Promise<T>
  }

  getSession(sessionId: string): Promise<SessionResponse> {
    return this.get(`/sessions/${encodeURIComponent(sessionId)}`)
  }

  getWorld(sessionId: string): Promise<WorldResponse> {
    return this.get(`/sessions/${encodeURIComponent(sessionId)}/world`)
  }

  getPlanetSurface(
    sessionId: string,
    planetId: string,
  ): Promise<SurfaceResponse> {
    return this.get(
      `/sessions/${encodeURIComponent(sessionId)}/planets/${encodeURIComponent(planetId)}/surface`,
    )
  }

  getPlanetTerrain(
    sessionId: string,
    planetId: string,
  ): Promise<TerrainResponse> {
    return this.get(
      `/sessions/${encodeURIComponent(sessionId)}/planets/${encodeURIComponent(planetId)}/terrain`,
    )
  }

  getPlanetHydrology(
    sessionId: string,
    planetId: string,
  ): Promise<HydrologyResponse> {
    return this.get(
      `/sessions/${encodeURIComponent(sessionId)}/planets/${encodeURIComponent(planetId)}/hydrology`,
    )
  }

  getPlanetStandingWater(
    sessionId: string,
    planetId: string,
  ): Promise<StandingWaterResponse> {
    return this.get(
      `/sessions/${encodeURIComponent(sessionId)}/planets/${encodeURIComponent(planetId)}/standing-water`,
    )
  }

  getTimeline(sessionId: string): Promise<TimelineResponse> {
    return this.get(
      `/sessions/${encodeURIComponent(sessionId)}/timeline`,
    )
  }

  advanceSession(
    sessionId: string,
    seconds: number,
  ): Promise<SessionResponse> {
    return this.post(
      `/sessions/${encodeURIComponent(sessionId)}/advance`,
      { seconds },
    )
  }
}
