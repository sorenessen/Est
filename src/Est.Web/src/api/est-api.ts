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
  activity: string
  energyReserve: number
  health: number
}

export interface WorldResponse {
  worldId: string
  currentTimeSeconds: number
  planets: PlanetResponse[]
  population: PopulationPersonResponse[]
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
