# ---------- build ----------
FROM node:20-alpine AS build
WORKDIR /app

# копируем манифесты (если lock появится — тоже попадёт)
COPY package*.json ./

# если есть package-lock.json → npm ci, иначе → npm install
RUN if [ -f package-lock.json ]; then \
      npm ci --no-audit --no-fund; \
    else \
      npm install --no-audit --no-fund; \
    fi

COPY . .
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

ARG VITE_API_PREFIX
ENV VITE_API_PREFIX=${VITE_API_PREFIX}
RUN npm run build

# ---------- serve ----------
FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
