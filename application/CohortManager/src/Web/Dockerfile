FROM node:22-alpine AS base

# Install dependencies only when needed
FROM base AS deps
# Check https://github.com/nodejs/docker-node/tree/b4117f9333da4138b03a546ec926ef50a31506c3#nodealpine to understand why libc6-compat might be needed.
RUN apk add --no-cache libc6-compat
WORKDIR /app

# Copy only the specific package manager files needed
COPY package.json ./
COPY ["package-lock.json", "./"]

# Install dependencies with npm
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

# Rebuild the source code only when needed
FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY tsconfig.json .
COPY app/ ./app/
COPY public/ ./public/
COPY proxy.ts .
COPY next.config.ts .
COPY package.json .
COPY package-lock.json .

# Build the application
RUN npm run build

# Production image, copy all the files and run next
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production

# Create necessary directories and set up user
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs && \
    mkdir -p /app/.next/standalone /app/.next/static

# Copy files from builder with secure permissions
COPY --from=builder --chown=root:root --chmod=444 /app/public ./public
COPY --from=builder --chown=root:root --chmod=444 /app/.next/standalone ./
COPY --from=builder --chown=root:root --chmod=444 /app/.next/static ./.next/static

# Set execute permissions only where needed
RUN chown -R root:root /app && \
    find /app -type d -exec chmod 555 {} + && \
    find /app -type f -name '*.js' -exec chmod 555 {} + && \
    find /app -type f -name '*.mjs' -exec chmod 555 {} + && \
    # Give nextjs user read access to run the application
    chown -R nextjs:nodejs /app/.next/standalone && \
    chown -R nextjs:nodejs /app/.next/static

USER nextjs

EXPOSE 3000

ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]
